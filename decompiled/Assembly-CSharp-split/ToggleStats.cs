using UnityEngine;

public class ToggleStats : MonoBehaviour
{
	public float movementSpeedMultiplier = 1f;

	public float hpMultiplier = 1f;

	private CharacterData data;

	private void Start()
	{
		data = GetComponentInParent<CharacterData>();
	}

	public void TurnOn()
	{
		data.health *= hpMultiplier;
		data.maxHealth *= hpMultiplier;
		data.stats.movementSpeed *= movementSpeedMultiplier;
		data.stats.ConfigureMassAndSize();
	}

	public void TurnOff()
	{
		data.health /= hpMultiplier;
		data.maxHealth /= hpMultiplier;
		data.stats.movementSpeed /= movementSpeedMultiplier;
		data.stats.ConfigureMassAndSize();
	}
}
