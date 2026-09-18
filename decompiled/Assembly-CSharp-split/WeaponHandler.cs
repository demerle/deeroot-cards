using System.Collections;
using Sonigon;
using UnityEngine;

public class WeaponHandler : MonoBehaviour
{
	[Header("Sounds")]
	public SoundEvent soundCharacterCantShoot;

	private bool soundFireHold;

	[Header("Settings")]
	public Gun gun;

	private Holding holding;

	private GeneralInput input;

	private CharacterData data;

	private float heatSinceAttack;

	private float heat;

	public float heatPerBullet = 0.1f;

	public float secondsBeforeStartToCool = 0.1f;

	public float coolPerSecond = 0.2f;

	public float overHeatTime = 1f;

	public float resetSpeed = 2f;

	public bool isOverHeated;

	private bool hasBeenHeated;

	public SpriteRenderer heatRenderer;

	private Transform overHeatPivot;

	public Color overHeatColor;

	private Color baseHeatColor;

	internal void DoReload()
	{
		gun.GetComponentInChildren<GunAmmo>().ReloadAmmo();
	}

	private void Awake()
	{
		holding = GetComponent<Holding>();
		input = GetComponent<GeneralInput>();
		data = GetComponent<CharacterData>();
	}

	private void Start()
	{
		overHeatPivot = heatRenderer.transform.parent;
		baseHeatColor = heatRenderer.color;
	}

	private void Update()
	{
		if (!gun.holdable.holder && (bool)data)
		{
			gun.holdable.holder = data;
		}
		if (data.playerVel.simulated)
		{
			gun.attackSpeedMultiplier = data.stats.attackSpeedMultiplier;
			heatSinceAttack += TimeHandler.deltaTime;
			Attack();
			OverHeat();
		}
	}

	private void Attack()
	{
		if (!gun || !gun.IsReady())
		{
			return;
		}
		if (input.shootIsPressed)
		{
			if (!soundFireHold)
			{
				soundFireHold = true;
				if (gun.isReloading || data.isSilenced)
				{
					SoundManager.Instance.Play(soundCharacterCantShoot, base.transform);
				}
			}
		}
		else
		{
			soundFireHold = false;
		}
		if (gun.bursts == 0 && (!soundFireHold || gun.isReloading || data.isSilenced))
		{
			gun.soundGun.StopAutoPlayTail();
		}
		if ((!input.shootWasPressed || gun.useCharge) && (!input.shootWasReleased || !gun.useCharge) && (!(gun.attackSpeed / data.stats.attackSpeedMultiplier < 0.3f) || !input.shootIsPressed || gun.useCharge || gun.dontAllowAutoFire))
		{
			return;
		}
		if (isOverHeated)
		{
			heatRenderer.GetComponent<CodeAnimation>().PlayBoop();
			gun.sinceAttack = 0f;
			return;
		}
		gun.Attack(0f);
		if (heat >= 1f)
		{
			StartCoroutine(DoOverHeat());
			isOverHeated = true;
		}
		heatSinceAttack = 0f;
	}

	internal void NewGun()
	{
		gun.ResetStats();
		gun.soundGun.ClearSoundModifiers();
	}

	private void OverHeat()
	{
		if (!isOverHeated)
		{
			if (heatSinceAttack > secondsBeforeStartToCool)
			{
				heat -= TimeHandler.deltaTime * coolPerSecond;
			}
			SetOverHeatColor();
		}
	}

	private IEnumerator DoOverHeat()
	{
		SetOverHeatColor();
		yield return new WaitForSeconds(overHeatTime);
		while (heat > 0f)
		{
			heat -= resetSpeed * TimeHandler.deltaTime;
			SetOverHeatColor();
			yield return null;
		}
		isOverHeated = false;
	}

	private void SetOverHeatColor()
	{
		heat = Mathf.Clamp(heat, 0f, 1f);
		heatRenderer.color = Color.Lerp(baseHeatColor, overHeatColor, heat);
		if (heat > 0.25f || hasBeenHeated)
		{
			hasBeenHeated = true;
			overHeatPivot.transform.localScale = new Vector3(heat, 1f, 1f);
		}
		else
		{
			overHeatPivot.transform.localScale = new Vector3(0f, 1f, 1f);
		}
		if (heat == 0f)
		{
			hasBeenHeated = false;
		}
	}
}
