using UnityEngine;

public class PlayerInRangeSlow : MonoBehaviour
{
	public float slowAmount = 0.1f;

	public float maxSlow = 0.5f;

	private PlayerInRangeTrigger trigger;

	private AttackLevel level;

	private void Start()
	{
		trigger = GetComponent<PlayerInRangeTrigger>();
		level = GetComponent<AttackLevel>();
	}

	private void Update()
	{
		if (trigger.inRange)
		{
			trigger.target.data.stats.AddSlowAddative(slowAmount * (float)level.attackLevel, maxSlow + ((float)level.attackLevel - 1f) * 0.25f);
		}
	}
}
