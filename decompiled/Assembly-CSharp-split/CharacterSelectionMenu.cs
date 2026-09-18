using System;
using UnityEngine;

public class CharacterSelectionMenu : MonoBehaviour
{
	private void Start()
	{
		PlayerManager instance = PlayerManager.instance;
		instance.PlayerJoinedAction = (Action<Player>)Delegate.Combine(instance.PlayerJoinedAction, new Action<Player>(PlayerJoined));
	}

	private void PlayerJoined(Player joinedPlayer)
	{
		base.transform.GetChild(0).GetChild(PlayerManager.instance.players.Count - 1).GetComponent<CharacterSelectionInstance>()
			.StartPicking(joinedPlayer);
	}

	private void Update()
	{
	}
}
