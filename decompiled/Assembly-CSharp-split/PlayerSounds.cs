using System;
using System.Collections.Generic;
using Sonigon;
using UnityEngine;

public class PlayerSounds : MonoBehaviour
{
	[Header("Sounds")]
	public SoundEvent soundCharacterJump;

	public SoundEvent soundCharacterJumpBig;

	public SoundEvent soundCharacterJumpEnsnare;

	public SoundEvent soundCharacterLand;

	public SoundEvent soundCharacterLandBig;

	private SoundParameterIntensity parameterIntensityLand = new SoundParameterIntensity(0f);

	public SoundEvent soundCharacterStickWall;

	public SoundEvent soundCharacterStickWallBig;

	private SoundParameterIntensity parameterIntensityStickWall = new SoundParameterIntensity(0f);

	public SoundEvent soundCharacterDamageScreenEdge;

	private CharacterData data;

	private List<EnsnareEffect> ensnareEffectList = new List<EnsnareEffect>();

	private bool ensnareEnabled;

	public void AddEnsnareEffect(EnsnareEffect ensnareEffect)
	{
		ensnareEffectList.Add(ensnareEffect);
	}

	public void RemoveEnsnareEffect(EnsnareEffect ensnareEffect)
	{
		ensnareEffectList.Remove(ensnareEffect);
	}

	private void Start()
	{
		data = GetComponent<CharacterData>();
		CharacterData characterData = data;
		characterData.TouchGroundAction = (Action<float, Vector3, Vector3, Transform>)Delegate.Combine(characterData.TouchGroundAction, new Action<float, Vector3, Vector3, Transform>(TouchGround));
		CharacterData characterData2 = data;
		characterData2.TouchWallAction = (Action<float, Vector3, Vector3>)Delegate.Combine(characterData2.TouchWallAction, new Action<float, Vector3, Vector3>(TouchWall));
		PlayerJump jump = data.jump;
		jump.JumpAction = (Action)Delegate.Combine(jump.JumpAction, new Action(Jump));
	}

	public void Jump()
	{
		ensnareEnabled = false;
		for (int num = ensnareEffectList.Count - 1; num >= 0; num--)
		{
			if (ensnareEffectList[num] == null)
			{
				ensnareEffectList.RemoveAt(num);
			}
		}
		for (int i = 0; i < ensnareEffectList.Count; i++)
		{
			if (ensnareEffectList[i].soundEnsnareJumpChange)
			{
				ensnareEnabled = true;
			}
		}
		if (ensnareEnabled)
		{
			if (ensnareEnabled)
			{
				SoundManager.Instance.Play(soundCharacterJumpEnsnare, base.transform);
			}
		}
		else if (data.stats.SoundTransformScaleThresholdReached())
		{
			SoundManager.Instance.Play(soundCharacterJumpBig, base.transform);
		}
		else
		{
			SoundManager.Instance.Play(soundCharacterJump, base.transform);
		}
	}

	public void TouchGround(float sinceGrounded, Vector3 pos, Vector3 normal, Transform ground)
	{
		if (sinceGrounded > 0.05f)
		{
			parameterIntensityLand.intensity = sinceGrounded;
			if (data.stats.SoundTransformScaleThresholdReached())
			{
				SoundManager.Instance.Play(soundCharacterLandBig, base.transform, parameterIntensityLand);
			}
			else
			{
				SoundManager.Instance.Play(soundCharacterLand, base.transform, parameterIntensityLand);
			}
		}
	}

	public void TouchWall(float sinceWall, Vector3 pos, Vector3 normal)
	{
		float num = sinceWall;
		if (data.sinceGrounded < num)
		{
			num = data.sinceGrounded;
		}
		if (num > 0.05f)
		{
			parameterIntensityStickWall.intensity = num;
			if (data.stats.SoundTransformScaleThresholdReached())
			{
				SoundManager.Instance.Play(soundCharacterStickWallBig, base.transform, parameterIntensityStickWall);
			}
			else
			{
				SoundManager.Instance.Play(soundCharacterStickWall, base.transform, parameterIntensityStickWall);
			}
		}
	}
}
