using UnityEngine;

public class RegenerateAfterStandStill : MonoBehaviour
{
	private CharacterData data;

	public float heal = 2f;

	private PlayerInRangeTrigger trigger;

	private void Start()
	{
		trigger = GetComponent<PlayerInRangeTrigger>();
		data = GetComponentInParent<CharacterData>();
	}

	private void Update()
	{
		if (trigger.inRange)
		{
			data.healthHandler.Heal(heal);
		}
	}
}
