using UnityEngine;
using UnityEngine.Events;

public class StatsAfterDealingDamage : MonoBehaviour
{
	public float duration = 3f;

	public float movementSpeedMultiplier = 1f;

	public float jumpMultiplier = 1f;

	public float hpMultiplier = 1f;

	public UnityEvent startEvent;

	public UnityEvent endEvent;

	private bool isOn;

	private CharacterData data;

	private void Start()
	{
		data = GetComponentInParent<CharacterData>();
	}

	private void Update()
	{
		bool flag = data.stats.sinceDealtDamage < duration;
		if (isOn != flag)
		{
			isOn = flag;
			_ = base.transform.localScale;
			if (isOn)
			{
				data.health *= hpMultiplier;
				data.maxHealth *= hpMultiplier;
				data.stats.movementSpeed *= movementSpeedMultiplier;
				data.stats.jump *= jumpMultiplier;
				data.stats.ConfigureMassAndSize();
				startEvent.Invoke();
			}
			else
			{
				data.health /= hpMultiplier;
				data.maxHealth /= hpMultiplier;
				data.stats.movementSpeed /= movementSpeedMultiplier;
				data.stats.jump /= jumpMultiplier;
				data.stats.ConfigureMassAndSize();
				endEvent.Invoke();
			}
		}
	}

	public void Interupt()
	{
		if (isOn)
		{
			data.health /= hpMultiplier;
			data.maxHealth /= hpMultiplier;
			data.stats.movementSpeed /= movementSpeedMultiplier;
			data.stats.jump /= jumpMultiplier;
			data.stats.ConfigureMassAndSize();
			endEvent.Invoke();
			isOn = false;
		}
	}
}
