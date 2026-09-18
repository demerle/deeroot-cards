using Sonigon;
using UnityEngine;

public class StatsWhenFullHP : MonoBehaviour
{
	public bool playSound;

	public SoundEvent soundPristineGrow;

	public SoundEvent soundPristineShrink;

	public float healthMultiplier = 1f;

	public float sizeMultiplier = 1f;

	public float healthThreshold = 0.95f;

	private CharacterData data;

	private bool isOn;

	private void Start()
	{
		data = GetComponentInParent<CharacterData>();
	}

	private void Update()
	{
		bool flag = data.health / data.maxHealth >= healthThreshold;
		if (flag == isOn)
		{
			return;
		}
		isOn = flag;
		if (isOn)
		{
			if (playSound)
			{
				SoundManager.Instance.PlayAtPosition(soundPristineGrow, SoundManager.Instance.GetTransform(), base.transform);
			}
			data.health *= healthMultiplier;
			data.maxHealth *= healthMultiplier;
			data.stats.sizeMultiplier *= sizeMultiplier;
			data.stats.ConfigureMassAndSize();
		}
		else
		{
			if (playSound)
			{
				SoundManager.Instance.PlayAtPosition(soundPristineShrink, SoundManager.Instance.GetTransform(), base.transform);
			}
			data.health /= healthMultiplier;
			data.maxHealth /= healthMultiplier;
			data.stats.sizeMultiplier /= sizeMultiplier;
			data.stats.ConfigureMassAndSize();
		}
	}
}
