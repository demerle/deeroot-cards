using UnityEngine;
using UnityEngine.Events;

public class EnableEvent : MonoBehaviour
{
	public UnityEvent enableEvent;

	public void OnEnable()
	{
		enableEvent.Invoke();
	}
}
