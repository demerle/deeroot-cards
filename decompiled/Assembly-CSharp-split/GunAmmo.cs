using Sonigon;
using UnityEngine;
using UnityEngine.UI;

public class GunAmmo : MonoBehaviour
{
	[Header("Sounds")]
	public SoundEvent soundReloadInProgressLoop;

	public SoundEvent soundReloadComplete;

	private SoundParameterIntensity soundReloadInProgressIntensity = new SoundParameterIntensity(0f, UpdateMode.Continuous);

	private bool soundReloadInProgressPlaying;

	private float soundReloadTime;

	[Header("Settings")]
	public int maxAmmo = 3;

	private int lastMaxAmmo;

	private int currentAmmo;

	public float reloadTime = 1.5f;

	public float reloadTimeMultiplier = 1f;

	public float reloadTimeAdd;

	private Gun gun;

	public CurveAnimation reloadAnim;

	private Image reloadRing;

	public Image cooldownRing;

	private float reloadCounter;

	private float freeReloadCounter;

	public Populate populate;

	public float ammoReg;

	private float currentRegCounter;

	private void Start()
	{
		reloadRing = reloadAnim.GetComponent<Image>();
		lastMaxAmmo = maxAmmo;
		currentAmmo = maxAmmo;
		gun = GetComponentInParent<Gun>();
		ReDrawTotalBullets();
	}

	private void OnDisable()
	{
		SoundStopReloadInProgress();
	}

	private void OnDestroy()
	{
		SoundStopReloadInProgress();
	}

	private void SoundStopReloadInProgress()
	{
		if (soundReloadInProgressPlaying)
		{
			soundReloadInProgressPlaying = false;
			SoundManager.Instance.Stop(soundReloadInProgressLoop, base.transform);
		}
	}

	private float ReloadTime()
	{
		return (reloadTime + reloadTimeAdd) * reloadTimeMultiplier;
	}

	private void Update()
	{
		if (!gun.isReloading)
		{
			SoundStopReloadInProgress();
		}
		if (gun.isReloading)
		{
			reloadCounter -= TimeHandler.deltaTime;
			if (!soundReloadInProgressPlaying)
			{
				soundReloadInProgressPlaying = true;
				soundReloadTime = ReloadTime();
				soundReloadInProgressIntensity.intensity = 0f;
				SoundManager.Instance.Play(soundReloadInProgressLoop, base.transform, soundReloadInProgressIntensity);
			}
			if (soundReloadInProgressPlaying && soundReloadTime > 0f)
			{
				soundReloadInProgressIntensity.intensity = 1f - reloadCounter / soundReloadTime;
			}
			if (reloadCounter < 0f)
			{
				ReloadAmmo();
			}
		}
		else if (currentAmmo != maxAmmo)
		{
			freeReloadCounter += TimeHandler.deltaTime;
			if (freeReloadCounter > ReloadTime() && gun.player.data.stats.automaticReload)
			{
				currentAmmo = maxAmmo;
				SetActiveBullets();
			}
			currentRegCounter += ammoReg * TimeHandler.deltaTime * (float)maxAmmo;
			if (currentRegCounter > 1f)
			{
				currentAmmo++;
				currentRegCounter = 0f;
				SetActiveBullets();
			}
		}
		if (currentAmmo <= 0)
		{
			if (reloadAnim.currentState != CurveAnimationUse.In)
			{
				reloadAnim.PlayIn();
			}
		}
		else if (reloadAnim.currentState != CurveAnimationUse.Out)
		{
			reloadAnim.PlayOut();
		}
		reloadRing.fillAmount = (ReloadTime() - reloadCounter) / ReloadTime();
		if (gun.attackSpeed > 0.4f)
		{
			cooldownRing.fillAmount = gun.ReadyAmount();
			if (gun.ReadyAmount() >= 1f)
			{
				cooldownRing.fillAmount = 0f;
			}
		}
		else
		{
			cooldownRing.fillAmount = 0f;
		}
		if (maxAmmo != lastMaxAmmo)
		{
			ReDrawTotalBullets();
		}
		lastMaxAmmo = maxAmmo;
	}

	public void ReloadAmmo(bool playSound = true)
	{
		gun.player.data.stats.OnReload(maxAmmo - currentAmmo);
		gun.isReloading = false;
		currentAmmo = maxAmmo;
		SoundStopReloadInProgress();
		if (playSound)
		{
			SoundManager.Instance.Play(soundReloadComplete, base.transform);
		}
		SetActiveBullets();
	}

	public void Shoot(GameObject projectile)
	{
		currentAmmo--;
		freeReloadCounter = 0f;
		SetActiveBullets();
		if (currentAmmo <= 0)
		{
			reloadCounter = ReloadTime();
			gun.isReloading = true;
			gun.player.data.stats.OnOutOfAmmp(maxAmmo);
		}
	}

	public void ReDrawTotalBullets()
	{
		currentAmmo = maxAmmo;
		for (int num = populate.transform.childCount - 1; num >= 0; num--)
		{
			if (populate.transform.GetChild(num).gameObject.activeSelf)
			{
				Object.Destroy(populate.transform.GetChild(num).gameObject);
			}
		}
		populate.times = maxAmmo;
		populate.DoPopulate();
		SetActiveBullets(forceTurnOn: true);
	}

	private void SetActiveBullets(bool forceTurnOn = false)
	{
		for (int i = 1; i < populate.transform.childCount; i++)
		{
			if (i <= currentAmmo || forceTurnOn)
			{
				if (populate.transform.GetChild(i).GetComponent<CurveAnimation>().currentState != CurveAnimationUse.In || forceTurnOn)
				{
					populate.transform.GetChild(i).GetComponent<CurveAnimation>().PlayIn();
				}
			}
			else if (populate.transform.GetChild(i).GetComponent<CurveAnimation>().currentState != CurveAnimationUse.Out)
			{
				populate.transform.GetChild(i).GetComponent<CurveAnimation>().PlayOut();
			}
		}
	}
}
