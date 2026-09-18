using System;
using System.Collections.Generic;
using Sonigon;
using UnityEngine;

namespace SoundImplementation;

[Serializable]
public class SoundGun
{
	private Gun parentGun;

	private Transform gunTransform;

	[Header("Sound Shot")]
	public int howManyShotModifiers = 3;

	[Range(-12f, 0f)]
	public float shotModifierLowerVolumeDbIf2 = -3f;

	[Range(-12f, 0f)]
	public float shotModifierLowerVolumeDbIf3 = -6f;

	public SoundShotModifier soundShotModifierBasic;

	private List<SoundShotModifier> soundShotModifierAllList = new List<SoundShotModifier>();

	private List<SoundShotModifier> soundShotModifierCurrentList = new List<SoundShotModifier>();

	private bool singleAutoIsPlaying;

	private bool shotgunAutoIsPlaying;

	[Header("Sound Impact")]
	public int howManyImpactModifiers = 2;

	[Range(-12f, 0f)]
	public float impactModifierLowerVolumeDbIf2 = -3f;

	[Range(-12f, 0f)]
	public float impactModifierLowerVolumeDbIf3 = -6f;

	public SoundImpactModifier soundImpactModifierBasic;

	public SoundImpactModifier soundImpactModifierDamageToExplosionMedium;

	public SoundImpactModifier soundImpactModifierDamageToExplosionHuge;

	private SoundParameterIntensity soundDamageToExplosionParameterIntensity = new SoundParameterIntensity(0f);

	public SoundEvent soundImpactBounce;

	public SoundEvent soundImpactBullet;

	private List<SoundImpactModifier> soundImpactModifierAllList = new List<SoundImpactModifier>();

	private List<SoundImpactModifier> soundImpactModifierCurrentList = new List<SoundImpactModifier>();

	private SoundParameterVolumeDecibel soundParameterVolumeDecibelShot = new SoundParameterVolumeDecibel();

	private SoundParameterVolumeDecibel soundParameterVolumeDecibelImpact = new SoundParameterVolumeDecibel();

	private bool autoFirstShot = true;

	private float autoStartTime;

	private float autoTimeSinceStart;

	private int autoShotsCalculate;

	private float autoDelayBetweenShotsDefault = 0.05f;

	private float autoDelayBetweenShotsNew;

	private SoundParameterPitchRatio autoPitchRatio = new SoundParameterPitchRatio(1f, UpdateMode.Continuous);

	private float autoPitchThresholdLow = 0.5f;

	private float autoPitchThresholdHigh = 3f;

	public void SetGun(Gun gun)
	{
		parentGun = gun;
	}

	public void SetGunTransform(Transform transform)
	{
		gunTransform = transform;
	}

	public void PlayImpact(HitInfo hit, RayHitReflect rayHitReflect)
	{
		bool flag = false;
		if (hit.transform != null)
		{
			if (hit.transform.tag == "Player")
			{
				flag = true;
			}
		}
		else
		{
			SoundManager.Instance.PlayAtPosition(soundImpactBullet, SoundManager.Instance.GetTransform(), hit.point, soundParameterVolumeDecibelImpact);
		}
		if (rayHitReflect != null && rayHitReflect.reflects > 0 && !flag)
		{
			SoundManager.Instance.PlayAtPosition(soundImpactBounce, SoundManager.Instance.GetTransform(), hit.point, soundParameterVolumeDecibelImpact);
		}
		else
		{
			PlayImpactModifiers(flag, hit.point);
		}
	}

	private void PlayImpactModifiers(bool isPlayer, Vector2 position)
	{
		for (int i = 0; i < soundImpactModifierCurrentList.Count; i++)
		{
			if (soundImpactModifierCurrentList[i] != null)
			{
				if (isPlayer)
				{
					SoundManager.Instance.PlayAtPosition(soundImpactModifierCurrentList[i].impactCharacter, SoundManager.Instance.GetTransform(), position, soundParameterVolumeDecibelImpact, soundDamageToExplosionParameterIntensity);
				}
				else
				{
					SoundManager.Instance.PlayAtPosition(soundImpactModifierCurrentList[i].impactEnvironment, SoundManager.Instance.GetTransform(), position, soundParameterVolumeDecibelImpact, soundDamageToExplosionParameterIntensity);
				}
			}
		}
	}

	public void ClearSoundModifiers()
	{
		soundShotModifierAllList.Clear();
		soundImpactModifierAllList.Clear();
		RefreshSoundModifiers();
	}

	public void AddSoundShotModifier(SoundShotModifier soundShotModifier)
	{
		if (soundShotModifier != null)
		{
			soundShotModifierAllList.Insert(0, soundShotModifier);
		}
	}

	public void AddSoundImpactModifier(SoundImpactModifier soundImpactModifier)
	{
		if (soundImpactModifier != null)
		{
			soundImpactModifierAllList.Insert(0, soundImpactModifier);
		}
	}

	public void RefreshSoundModifiers()
	{
		StopAutoPlayTail();
		if (howManyShotModifiers < 1)
		{
			howManyShotModifiers = 1;
		}
		if (howManyImpactModifiers < 1)
		{
			howManyImpactModifiers = 1;
		}
		int num = 0;
		for (int i = 0; i < soundShotModifierAllList.Count; i++)
		{
			if (num < soundShotModifierAllList[i].priority)
			{
				num = soundShotModifierAllList[i].priority;
			}
		}
		soundShotModifierCurrentList.Clear();
		int num2 = 0;
		for (int num3 = num; num3 >= 0; num3--)
		{
			for (int j = 0; j < soundShotModifierAllList.Count; j++)
			{
				if (soundShotModifierAllList[j].priority == num3 && !soundShotModifierCurrentList.Contains(soundShotModifierAllList[j]))
				{
					soundShotModifierCurrentList.Add(soundShotModifierAllList[j]);
					num2++;
				}
				if (num2 >= howManyShotModifiers)
				{
					break;
				}
			}
			if (num2 >= howManyShotModifiers)
			{
				break;
			}
		}
		if (soundShotModifierCurrentList.Count == 0)
		{
			soundShotModifierCurrentList.Add(soundShotModifierBasic);
		}
		if (soundShotModifierCurrentList.Count == 1)
		{
			soundParameterVolumeDecibelShot.volumeDecibel = 0f;
		}
		else if (soundShotModifierCurrentList.Count == 2)
		{
			soundParameterVolumeDecibelShot.volumeDecibel = shotModifierLowerVolumeDbIf2;
		}
		else if (soundShotModifierCurrentList.Count == 3)
		{
			soundParameterVolumeDecibelShot.volumeDecibel = shotModifierLowerVolumeDbIf3;
		}
		soundDamageToExplosionParameterIntensity.intensity = parentGun.damage;
		if (soundImpactModifierDamageToExplosionMedium != null && soundImpactModifierDamageToExplosionHuge != null)
		{
			if (parentGun.damage > 2.6f)
			{
				if (parentGun.damage > 8f)
				{
					if (!soundImpactModifierAllList.Contains(soundImpactModifierDamageToExplosionHuge))
					{
						soundImpactModifierAllList.Insert(0, soundImpactModifierDamageToExplosionHuge);
					}
					soundImpactModifierAllList.Remove(soundImpactModifierDamageToExplosionMedium);
				}
				else
				{
					if (!soundImpactModifierAllList.Contains(soundImpactModifierDamageToExplosionMedium))
					{
						soundImpactModifierAllList.Insert(0, soundImpactModifierDamageToExplosionMedium);
					}
					soundImpactModifierAllList.Remove(soundImpactModifierDamageToExplosionHuge);
				}
			}
			else
			{
				soundImpactModifierAllList.Remove(soundImpactModifierDamageToExplosionMedium);
				soundImpactModifierAllList.Remove(soundImpactModifierDamageToExplosionHuge);
			}
		}
		int num4 = 0;
		for (int k = 0; k < soundImpactModifierAllList.Count; k++)
		{
			if (num4 < soundImpactModifierAllList[k].priority)
			{
				num4 = soundImpactModifierAllList[k].priority;
			}
		}
		soundImpactModifierCurrentList.Clear();
		int num5 = 0;
		for (int num6 = num4; num6 >= 0; num6--)
		{
			for (int l = 0; l < soundImpactModifierAllList.Count; l++)
			{
				if (soundImpactModifierAllList[l].priority == num6 && !soundImpactModifierCurrentList.Contains(soundImpactModifierAllList[l]))
				{
					soundImpactModifierCurrentList.Add(soundImpactModifierAllList[l]);
					num5++;
				}
				if (num5 >= howManyImpactModifiers)
				{
					break;
				}
			}
			if (num5 >= howManyImpactModifiers)
			{
				break;
			}
		}
		if (soundImpactModifierCurrentList.Count == 0)
		{
			soundImpactModifierCurrentList.Add(soundImpactModifierBasic);
		}
		if (soundImpactModifierCurrentList.Count == 1)
		{
			soundParameterVolumeDecibelImpact.volumeDecibel = 0f;
		}
		else if (soundImpactModifierCurrentList.Count == 2)
		{
			soundParameterVolumeDecibelImpact.volumeDecibel = impactModifierLowerVolumeDbIf2;
		}
		else if (soundImpactModifierCurrentList.Count == 3)
		{
			soundParameterVolumeDecibelImpact.volumeDecibel = impactModifierLowerVolumeDbIf3;
		}
	}

	public void StopAutoPlayTail()
	{
		autoFirstShot = true;
		StopInternalSingleAutoPlayTail();
		StopInternalShotgunAutoPlayTail();
	}

	private void StopInternalSingleAutoPlayTail()
	{
		if (!singleAutoIsPlaying)
		{
			return;
		}
		singleAutoIsPlaying = false;
		for (int i = 0; i < soundShotModifierCurrentList.Count; i++)
		{
			if (soundShotModifierCurrentList[i] != null)
			{
				SoundManager.Instance.Stop(soundShotModifierCurrentList[i].singleAutoLoop, gunTransform);
				SoundManager.Instance.Play(soundShotModifierCurrentList[i].singleAutoTail, gunTransform, soundParameterVolumeDecibelShot, autoPitchRatio);
			}
		}
	}

	private void StopInternalShotgunAutoPlayTail()
	{
		if (!shotgunAutoIsPlaying)
		{
			return;
		}
		shotgunAutoIsPlaying = false;
		for (int i = 0; i < soundShotModifierCurrentList.Count; i++)
		{
			if (soundShotModifierCurrentList[i] != null)
			{
				SoundManager.Instance.Stop(soundShotModifierCurrentList[i].shotgunAutoLoop, gunTransform);
				SoundManager.Instance.Play(soundShotModifierCurrentList[i].shotgunAutoTail, gunTransform, soundParameterVolumeDecibelShot);
			}
		}
	}

	private void PlaySingle()
	{
		StopInternalSingleAutoPlayTail();
		StopInternalShotgunAutoPlayTail();
		for (int i = 0; i < soundShotModifierCurrentList.Count; i++)
		{
			if (soundShotModifierCurrentList[i] != null)
			{
				SoundManager.Instance.Play(soundShotModifierCurrentList[i].single, gunTransform, soundParameterVolumeDecibelShot);
			}
		}
	}

	private void PlayShotgun()
	{
		StopInternalSingleAutoPlayTail();
		StopInternalShotgunAutoPlayTail();
		for (int i = 0; i < soundShotModifierCurrentList.Count; i++)
		{
			if (soundShotModifierCurrentList[i] != null)
			{
				SoundManager.Instance.Play(soundShotModifierCurrentList[i].shotgun, gunTransform, soundParameterVolumeDecibelShot);
			}
		}
	}

	private void PlaySingleAuto()
	{
		StopInternalShotgunAutoPlayTail();
		if (singleAutoIsPlaying)
		{
			return;
		}
		singleAutoIsPlaying = true;
		for (int i = 0; i < soundShotModifierCurrentList.Count; i++)
		{
			if (soundShotModifierCurrentList[i] != null)
			{
				SoundManager.Instance.Play(soundShotModifierCurrentList[i].singleAutoLoop, gunTransform, soundParameterVolumeDecibelShot, autoPitchRatio);
			}
		}
	}

	private void PlayShotgunAuto()
	{
		StopInternalSingleAutoPlayTail();
		if (shotgunAutoIsPlaying)
		{
			return;
		}
		shotgunAutoIsPlaying = true;
		for (int i = 0; i < soundShotModifierCurrentList.Count; i++)
		{
			if (soundShotModifierCurrentList[i] != null)
			{
				SoundManager.Instance.Play(soundShotModifierCurrentList[i].shotgunAutoLoop, gunTransform, soundParameterVolumeDecibelShot);
			}
		}
	}

	public void PlayShot(int currentNumberOfProjectiles)
	{
		if ((parentGun.bursts > 0 && !parentGun.useCharge && !parentGun.dontAllowAutoFire) || (parentGun.attackSpeed / parentGun.attackSpeedMultiplier < 0.15f && !parentGun.useCharge && !parentGun.dontAllowAutoFire))
		{
			if (currentNumberOfProjectiles > 1)
			{
				PlayShotgunAuto();
				return;
			}
			if (autoFirstShot)
			{
				autoFirstShot = false;
				autoShotsCalculate = 0;
				autoStartTime = Time.realtimeSinceStartup;
			}
			else
			{
				autoShotsCalculate++;
				autoTimeSinceStart = Time.realtimeSinceStartup - autoStartTime;
				autoDelayBetweenShotsNew = autoTimeSinceStart / (float)autoShotsCalculate;
				autoPitchRatio.pitchRatio = autoDelayBetweenShotsDefault / autoDelayBetweenShotsNew;
			}
			if (autoPitchRatio.pitchRatio < autoPitchThresholdLow)
			{
				if (!singleAutoIsPlaying && !shotgunAutoIsPlaying)
				{
					PlaySingle();
				}
			}
			else if (autoPitchRatio.pitchRatio > autoPitchThresholdHigh)
			{
				PlayShotgunAuto();
			}
			else if (!shotgunAutoIsPlaying)
			{
				PlaySingleAuto();
			}
		}
		else if (currentNumberOfProjectiles > 1)
		{
			PlayShotgun();
		}
		else
		{
			PlaySingle();
		}
	}
}
