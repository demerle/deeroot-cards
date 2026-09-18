using UnityEngine;
using UnityEngine.Events;

public class CooldownCondition : MonoBehaviour
{
	public UnityEvent triggerEvent;

	public float cooldown = 0.25f;

	private float lastTime = -100f;

	public void TryEvent()
	{
		if (!(Time.time < lastTime + cooldown))
		{
			lastTime = Time.time;
			triggerEvent.Invoke();
		}
	}
}
