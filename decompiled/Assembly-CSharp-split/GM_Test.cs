using System;
using System.Collections;
using UnityEngine;

public class GM_Test : MonoBehaviour
{
	public bool testMap;

	public static GM_Test instance;

	private void Awake()
	{
		instance = this;
		if (base.gameObject.activeSelf && !Application.isEditor)
		{
			testMap = false;
		}
	}

	private void Start()
	{
		if (testMap)
		{
			base.transform.root.GetComponent<SetOfflineMode>().SetOffline();
		}
		if (testMap)
		{
			MapManager.instance.isTestingMap = true;
		}
		if (base.gameObject.activeSelf)
		{
			if (!testMap)
			{
				MapManager.instance.LoadNextLevel(callInImidetly: true, forceLoad: true);
			}
			else
			{
				MapManager.instance.currentMap = new MapWrapper(UnityEngine.Object.FindObjectOfType<Map>(), UnityEngine.Object.FindObjectOfType<Map>().gameObject.scene);
				ArtHandler.instance.NextArt();
			}
			PlayerAssigner.instance.SetPlayersCanJoin(canJoin: true);
			TimeHandler.instance.StartGame();
			PlayerManager playerManager = PlayerManager.instance;
			playerManager.PlayerJoinedAction = (Action<Player>)Delegate.Combine(playerManager.PlayerJoinedAction, new Action<Player>(PlayerWasAdded));
			PlayerManager.instance.AddPlayerDiedAction(PlayerDied);
			GameManager.instance.isPlaying = true;
			GameManager.instance.battleOngoing = true;
		}
	}

	private void PlayerWasAdded(Player player)
	{
		PlayerManager.instance.SetPlayersSimulated(simulated: true);
		player.data.GetComponent<PlayerCollision>().IgnoreWallForFrames(2);
		player.transform.position = MapManager.instance.currentMap.Map.GetRandomSpawnPos();
		PlayerManager.instance.SetPlayersSimulated(simulated: true);
		PlayerManager.instance.SetPlayersPlaying(playing: true);
	}

	private void PlayerDied(Player player, int unused)
	{
		StartCoroutine(DelayRevive(player));
	}

	private IEnumerator DelayRevive(Player player)
	{
		yield return new WaitForSecondsRealtime(2.5f);
		PlayerWasAdded(player);
		player.data.healthHandler.Revive();
	}
}
