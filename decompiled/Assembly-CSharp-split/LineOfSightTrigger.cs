using System;
using UnityEngine;
using UnityEngine.Events;

public class LineOfSightTrigger : MonoBehaviour
{
	public UnityEvent turnOnEvent;

	public UnityEvent turnOffEvent;

	public UnityEvent switchTargetEvent;

	public Action<Player> turnOnAction;

	public Action<Player> switchTargetAction;

	public Action turnOffAction;

	private Player player;

	private bool isOn;

	private Player currentTarget;

	private void Start()
	{
		player = GetComponentInParent<Player>();
	}

	private void Update()
	{
		Player closestPlayerInTeam = PlayerManager.instance.GetClosestPlayerInTeam(base.transform.position, PlayerManager.instance.GetOtherTeam(player.teamID));
		if ((bool)closestPlayerInTeam)
		{
			if (currentTarget != player)
			{
				switchTargetEvent.Invoke();
				switchTargetAction?.Invoke(player);
				currentTarget = closestPlayerInTeam;
			}
			if (!isOn)
			{
				isOn = true;
				turnOnAction?.Invoke(player);
				turnOnEvent.Invoke();
			}
		}
		else
		{
			if (isOn)
			{
				isOn = false;
				turnOffAction?.Invoke();
				turnOffEvent.Invoke();
			}
			currentTarget = null;
		}
	}
}
