using UnityEngine;
using UnityEngine.Events;

public class JustEvent : MonoBehaviour
{
	public UnityEvent justEvent;

	public void Go()
	{
		justEvent.Invoke();
	}
}
