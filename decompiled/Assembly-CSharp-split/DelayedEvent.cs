using System;
using UnityEngine.Events;

[Serializable]
public class DelayedEvent
{
	public UnityEvent eventTrigger;

	public float delay;

	public int cycles = 1;

	public int cyclesPerLvl;
}
