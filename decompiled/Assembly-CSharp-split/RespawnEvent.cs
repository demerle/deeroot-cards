using System;
using UnityEngine;
using UnityEngine.Events;

public class RespawnEvent : MonoBehaviour
{
	public UnityEvent reviveEvent;

	private void Start()
	{
		HealthHandler healthHandler = GetComponentInParent<Player>().data.healthHandler;
		healthHandler.reviveAction = (Action)Delegate.Combine(healthHandler.reviveAction, new Action(DoEvent));
	}

	public void DoEvent()
	{
		reviveEvent.Invoke();
	}
}
