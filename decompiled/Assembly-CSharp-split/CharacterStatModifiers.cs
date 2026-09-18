using System;
using System.Collections.Generic;
using Photon.Pun;
using Sonigon;
using UnityEngine;

public class CharacterStatModifiers : MonoBehaviour
{
	[Header("Sounds")]
	public SoundEvent soundCharacterSlowFreeze;

	private float soundSlowTime;

	private float soundSlowSpeedSec = 0.3f;

	[Header("Settings")]
	public GameObject AddObjectToPlayer;

	[HideInInspector]
	public List<GameObject> objectsAddedToPlayer;

	[Header("Multiply")]
	public float sizeMultiplier = 1f;

	public float health = 1f;

	public float movementSpeed = 1f;

	public float jump = 1f;

	public float gravity = 1f;

	public float slow;

	public float slowSlow;

	public float fastSlow;

	[Header("Add")]
	public float secondsToTakeDamageOver;

	public int numberOfJumps;

	public float regen;

	public float lifeSteal;

	public bool refreshOnDamage;

	public bool automaticReload = true;

	public int respawns;

	[HideInInspector]
	public int remainingRespawns;

	[HideInInspector]
	public float tasteOfBloodSpeed = 1f;

	[HideInInspector]
	public float rageSpeed = 1f;

	public float attackSpeedMultiplier = 1f;

	private WasDealtDamageEffect[] wasDealtDamageEffects;

	private DealtDamageEffect[] dealtDamageEffects;

	private CharacterData data;

	public ParticleSystem slowPart;

	private float soundBigThreshold = 1.5f;

	public Action<Vector2, bool> DealtDamageAction;

	public Action<Vector2, bool> WasDealtDamageAction;

	public Action<int> OnReloadDoneAction;

	public Action<int> OutOfAmmpAction;

	internal float sinceDealtDamage;

	public bool SoundTransformScaleThresholdReached()
	{
		if (base.transform.localScale.x > soundBigThreshold)
		{
			return true;
		}
		return false;
	}

	private void Start()
	{
		data = GetComponent<CharacterData>();
	}

	public float GetSlow()
	{
		return Mathf.Clamp(slow, 0f, 0.9f);
	}

	private void Update()
	{
		attackSpeedMultiplier = tasteOfBloodSpeed * rageSpeed;
		sinceDealtDamage += TimeHandler.deltaTime;
		if ((bool)data && !data.isPlaying)
		{
			sinceDealtDamage = 100f;
		}
		slow = slowSlow;
		if (fastSlow > slowSlow)
		{
			slow = fastSlow;
		}
		if (slowSlow > 0f)
		{
			slowSlow = Mathf.Clamp(slowSlow - TimeHandler.deltaTime * 0.3f, 0f, 10f);
		}
		if (fastSlow > 0f)
		{
			fastSlow = Mathf.Clamp(fastSlow - TimeHandler.deltaTime * 2f, 0f, 1f);
		}
	}

	public void DealtDamage(Vector2 damage, bool selfDamage, Player damagedPlayer = null)
	{
		if (lifeSteal != 0f && !selfDamage)
		{
			GetComponent<HealthHandler>().Heal(damage.magnitude * lifeSteal);
		}
		if (refreshOnDamage)
		{
			GetComponent<Holding>().holdable.GetComponent<Weapon>().sinceAttack = float.PositiveInfinity;
		}
		if (DealtDamageAction != null)
		{
			DealtDamageAction(damage, selfDamage);
		}
		if ((bool)damagedPlayer)
		{
			data.lastDamagedPlayer = damagedPlayer;
		}
		if (!selfDamage)
		{
			sinceDealtDamage = 0f;
		}
		if (dealtDamageEffects != null)
		{
			for (int i = 0; i < dealtDamageEffects.Length; i++)
			{
				dealtDamageEffects[i].DealtDamage(damage, selfDamage, damagedPlayer);
			}
		}
	}

	internal void ResetStats()
	{
		for (int i = 0; i < objectsAddedToPlayer.Count; i++)
		{
			UnityEngine.Object.Destroy(objectsAddedToPlayer[i]);
		}
		objectsAddedToPlayer.Clear();
		data.health = 100f;
		data.maxHealth = 100f;
		sizeMultiplier = 1f;
		health = 1f;
		movementSpeed = 1f;
		jump = 1f;
		gravity = 1f;
		slow = 0f;
		slowSlow = 0f;
		fastSlow = 0f;
		secondsToTakeDamageOver = 0f;
		numberOfJumps = 0;
		regen = 0f;
		lifeSteal = 0f;
		respawns = 0;
		refreshOnDamage = false;
		automaticReload = true;
		tasteOfBloodSpeed = 1f;
		rageSpeed = 1f;
		attackSpeedMultiplier = 1f;
		WasUpdated();
		ConfigureMassAndSize();
	}

	public void WasDealtDamage(Vector2 damage, bool selfDamage)
	{
		if (WasDealtDamageAction != null)
		{
			WasDealtDamageAction(damage, selfDamage);
		}
		if (wasDealtDamageEffects != null)
		{
			for (int i = 0; i < wasDealtDamageEffects.Length; i++)
			{
				wasDealtDamageEffects[i].WasDealtDamage(damage, selfDamage);
			}
		}
	}

	public void WasUpdated()
	{
		GetComponent<ForceMultiplier>().multiplier = 1f / base.transform.root.localScale.x;
		wasDealtDamageEffects = GetComponentsInChildren<WasDealtDamageEffect>();
		dealtDamageEffects = GetComponentsInChildren<DealtDamageEffect>();
	}

	public void AddSlowAddative(float slowToAdd, float maxValue = 1f, bool isFastSlow = false)
	{
		if (data.block.IsBlocking())
		{
			return;
		}
		DoSlowDown(slowToAdd);
		if (isFastSlow)
		{
			if (fastSlow < maxValue)
			{
				fastSlow += slowToAdd;
				fastSlow = Mathf.Clamp(slow, 0f, maxValue);
			}
		}
		else if (slowSlow < maxValue)
		{
			slowSlow += slowToAdd;
			slowSlow = Mathf.Clamp(slowSlow, 0f, maxValue);
		}
	}

	[PunRPC]
	public void RPCA_AddSlow(float slowToAdd, bool isFastSlow = false)
	{
		DoSlowDown(slowToAdd);
		if (isFastSlow)
		{
			fastSlow = Mathf.Clamp(fastSlow, slowToAdd, 1f);
		}
		else
		{
			slowSlow = Mathf.Clamp(slowSlow, slowToAdd, 10f);
		}
	}

	private void DoSlowDown(float newSlow)
	{
		if (soundSlowTime + soundSlowSpeedSec < Time.time)
		{
			soundSlowTime = Time.time;
			SoundManager.Instance.Play(soundCharacterSlowFreeze, base.transform);
		}
		float num = Mathf.Clamp(newSlow - slow, 0f, 1f);
		slowPart.Emit((int)Mathf.Clamp((newSlow * 0.1f + num * 0.7f) * 50f, 1f, 50f));
		data.playerVel.velocity *= 1f - num * 1f;
		data.sinceGrounded *= 1f - num * 1f;
	}

	internal void ConfigureMassAndSize()
	{
		base.transform.localScale = Vector3.one * 1.2f * Mathf.Pow(data.maxHealth / 100f * 1.2f, 0.2f) * sizeMultiplier;
		data.playerVel.mass = 100f * Mathf.Pow(data.maxHealth / 100f * 1.2f, 0.8f) * sizeMultiplier;
	}

	internal void OnReload(int bulletsReloaded)
	{
		if (OnReloadDoneAction != null)
		{
			OnReloadDoneAction(bulletsReloaded);
		}
	}

	internal void OnOutOfAmmp(int maxAmmo)
	{
		if (OutOfAmmpAction != null)
		{
			OutOfAmmpAction(maxAmmo);
		}
	}
}
