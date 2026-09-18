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
				Action<Player> action = switchTargetAction;
				if (action != null)
				{
					action(player);
				}
				currentTarget = closestPlayerInTeam;
			}
			if (!isOn)
			{
				isOn = true;
				Action<Player> action2 = turnOnAction;
				if (action2 != null)
				{
					action2(player);
				}
				turnOnEvent.Invoke();
			}
			return;
		}
		if (isOn)
		{
			isOn = false;
			Action action3 = turnOffAction;
			if (action3 != null)
			{
				action3();
			}
			turnOffEvent.Invoke();
		}
		currentTarget = null;
	}
}
