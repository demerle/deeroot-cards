using UnityEngine;
using UnityEngine.Events;

public class DisableEvent : MonoBehaviour
{
	public UnityEvent disableEvent;

	public void OnDisable()
	{
		disableEvent.Invoke();
	}
}
