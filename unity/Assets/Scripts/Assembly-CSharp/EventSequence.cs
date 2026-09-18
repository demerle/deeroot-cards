using System.Collections;
using UnityEngine;

public class EventSequence : MonoBehaviour
{
	public bool playOnAwake = true;

	public bool useTimeScale = true;

	public DelayedEvent[] events;

	private int level;

	private void Start()
	{
		level = GetComponentInParent<AttackLevel>().attackLevel;
		if (playOnAwake)
		{
			Go();
		}
	}

	public void Go()
	{
		StartCoroutine(DoSequence());
	}

	private IEnumerator DoSequence()
	{
		for (int i = 0; i < events.Length; i++)
		{
			float num = 0f;
			for (int i2 = 0; i2 < events[i].cycles + events[i].cyclesPerLvl * level; i2++)
			{
				num += events[i].delay;
				if (num > TimeHandler.deltaTime)
				{
					if (useTimeScale)
					{
						yield return new WaitForSeconds(num);
					}
					else
					{
						yield return new WaitForSecondsRealtime(num);
					}
					num = 0f;
				}
				events[i].eventTrigger.Invoke();
			}
		}
	}
}
