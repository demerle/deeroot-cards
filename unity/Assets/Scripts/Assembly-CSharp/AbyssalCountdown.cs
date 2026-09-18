using System;
using Sonigon;
using UnityEngine;
using UnityEngine.UI.ProceduralImage;

public class AbyssalCountdown : MonoBehaviour
{
	public SoundEvent soundAbyssalChargeLoop;

	private bool soundChargeIsPlaying;

	private float soundCounterLast;

	private SoundParameterIntensity soundParameterIntensity = new SoundParameterIntensity(0f, UpdateMode.Continuous);

	[Range(0f, 1f)]
	public float counter;

	public float timeToFill = 10f;

	public float timeToEmpty = 3f;

	public float duration;

	public float hpMultiplier = 2f;

	public ProceduralImage outerRing;

	public ProceduralImage fill;

	public Transform rotator;

	public Transform still;

	private CharacterData data;

	public GameObject[] abyssalObjects;

	private float remainingDuration;

	private bool isAbyssalForm;

	private float startCounter;

	private void Start()
	{
		soundCounterLast = counter;
		data = GetComponentInParent<CharacterData>();
		HealthHandler healthHandler = data.healthHandler;
		healthHandler.reviveAction = (Action)Delegate.Combine(healthHandler.reviveAction, new Action(ResetStuff));
		GetComponentInParent<ChildRPC>().childRPCs.Add("Abyssal", RPCA_Activate);
	}

	private void OnDestroy()
	{
		HealthHandler healthHandler = data.healthHandler;
		healthHandler.reviveAction = (Action)Delegate.Combine(healthHandler.reviveAction, new Action(ResetStuff));
		GetComponentInParent<ChildRPC>().childRPCs.Remove("Abyssal");
		SoundStop();
	}

	private void OnDisable()
	{
		SoundStop();
	}

	private void SoundPlay()
	{
		if (!soundChargeIsPlaying)
		{
			soundChargeIsPlaying = true;
			SoundManager.Instance.Play(soundAbyssalChargeLoop, base.transform, soundParameterIntensity);
		}
	}

	private void SoundStop()
	{
		if (soundChargeIsPlaying)
		{
			soundChargeIsPlaying = false;
			SoundManager.Instance.Stop(soundAbyssalChargeLoop, base.transform);
		}
	}

	private void ResetStuff()
	{
		SoundStop();
		remainingDuration = 0f;
		counter = 0f;
		if (isAbyssalForm)
		{
			for (int i = 0; i < abyssalObjects.Length; i++)
			{
				abyssalObjects[i].gameObject.SetActive(false);
			}
			data.maxHealth /= hpMultiplier;
			data.health /= hpMultiplier;
			data.stats.ConfigureMassAndSize();
			isAbyssalForm = false;
			rotator.gameObject.SetActive(false);
			still.gameObject.SetActive(false);
		}
		SoundStop();
	}

	private void RPCA_Activate()
	{
		remainingDuration = duration;
	}

	private void Update()
	{
		if (soundCounterLast < counter)
		{
			SoundPlay();
		}
		else
		{
			SoundStop();
		}
		soundCounterLast = counter;
		soundParameterIntensity.intensity = counter;
		outerRing.fillAmount = counter;
		fill.fillAmount = counter;
		rotator.transform.localEulerAngles = new Vector3(0f, 0f, 0f - Mathf.Lerp(0f, 360f, counter));
		if (!data.playerVel.simulated)
		{
			startCounter = 1f;
			return;
		}
		startCounter -= TimeHandler.deltaTime;
		if (startCounter > 0f)
		{
			return;
		}
		if (remainingDuration > 0f)
		{
			if (!isAbyssalForm)
			{
				for (int i = 0; i < abyssalObjects.Length; i++)
				{
					abyssalObjects[i].gameObject.SetActive(true);
				}
				data.maxHealth *= hpMultiplier;
				data.health *= hpMultiplier;
				data.stats.ConfigureMassAndSize();
				isAbyssalForm = true;
			}
			remainingDuration -= TimeHandler.deltaTime;
			counter = remainingDuration / duration;
			return;
		}
		if (isAbyssalForm)
		{
			for (int j = 0; j < abyssalObjects.Length; j++)
			{
				abyssalObjects[j].gameObject.SetActive(false);
			}
			data.maxHealth /= hpMultiplier;
			data.health /= hpMultiplier;
			data.stats.ConfigureMassAndSize();
			isAbyssalForm = false;
		}
		if (data.input.direction == Vector3.zero || data.input.direction == Vector3.down)
		{
			counter += TimeHandler.deltaTime / timeToFill;
		}
		else
		{
			counter -= TimeHandler.deltaTime / timeToEmpty;
		}
		counter = Mathf.Clamp(counter, -0.1f / timeToFill, 1f);
		if (counter >= 1f && data.view.IsMine)
		{
			remainingDuration = duration;
			GetComponentInParent<ChildRPC>().CallFunction("Abyssal");
		}
		if (counter <= 0f)
		{
			rotator.gameObject.SetActive(false);
			still.gameObject.SetActive(false);
		}
		else
		{
			rotator.gameObject.SetActive(true);
			still.gameObject.SetActive(true);
		}
	}
}
