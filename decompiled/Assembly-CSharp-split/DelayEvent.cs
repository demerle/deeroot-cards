using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class DelayEvent : MonoBehaviour
{
	public UnityEvent delayedEvent;

	public float time = 1f;

	public bool auto;

	public bool repeating;

	public bool usedTimeScale = true;

	private void Start()
	{
		CodeAnimation componentInParent = GetComponentInParent<CodeAnimation>();
		if ((bool)componentInParent)
		{
			time /= componentInParent.animations[0].animationSpeed;
		}
		if (auto)
		{
			Go();
		}
	}

	public void Go()
	{
		StartCoroutine(DelayEventCall());
	}

	private IEnumerator DelayEventCall()
	{
		yield return 1;
		if (usedTimeScale)
		{
			yield return new WaitForSeconds(time);
		}
		else
		{
			yield return new WaitForSecondsRealtime(time);
		}
		if (base.enabled)
		{
			delayedEvent.Invoke();
			if (repeating)
			{
				Go();
			}
		}
	}

	public void DoEvent()
	{
		StopAllCoroutines();
		delayedEvent.Invoke();
	}
}
