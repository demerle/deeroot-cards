using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Sonigon;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
	[Header("Sounds")]
	public SoundEvent[] soundCharacterSpawn;

	public static PlayerManager instance;

	public LayerMask canSeePlayerMask;

	public List<Player> players = new List<Player>();

	public PhotonView view;

	private Action<Player, int> PlayerDiedAction;

	private bool playersShouldBeActive;

	public AnimationCurve playerMoveCurve;

	public Action<Player> PlayerJoinedAction { get; internal set; }

	private void Awake()
	{
		instance = this;
		view = GetComponent<PhotonView>();
	}

	public Player GetOtherPlayer(Player asker)
	{
		return GetClosestPlayerInTeam(asker.transform.position, GetOtherTeam(asker.teamID));
	}

	public Player GetClosestPlayer(Vector2 refPos, bool needVision = false)
	{
		Player result = null;
		float num = float.PositiveInfinity;
		for (int i = 0; i < players.Count; i++)
		{
			if (!players[i].data.dead)
			{
				float num2 = Vector2.Distance(refPos, players[i].data.playerVel.position);
				if ((!needVision || CanSeePlayer(refPos, players[i]).canSee) && num2 < num)
				{
					num = num2;
					result = players[i];
				}
			}
		}
		return result;
	}

	internal Player GetPlayerWithActorID(int actorID)
	{
		for (int i = 0; i < players.Count; i++)
		{
			if (players[i].data.view.OwnerActorNr == actorID)
			{
				return players[i];
			}
		}
		return null;
	}

	public Player GetClosestPlayerInTeam(Vector3 position, int team, bool needVision = false)
	{
		float num = float.MaxValue;
		Player[] playersInTeam = GetPlayersInTeam(team);
		Player result = null;
		for (int i = 0; i < playersInTeam.Length; i++)
		{
			if (!players[i].data.dead)
			{
				float num2 = Vector2.Distance(position, playersInTeam[i].transform.position);
				if ((!needVision || CanSeePlayer(position, playersInTeam[i]).canSee) && num2 < num)
				{
					num = num2;
					result = playersInTeam[i];
				}
			}
		}
		return result;
	}

	public Player GetClosestPlayer(Vector2 refPos, Vector2 forward)
	{
		Player result = null;
		float num = float.PositiveInfinity;
		for (int i = 0; i < players.Count; i++)
		{
			if (!players[i].data.dead && CanSeePlayer(refPos, players[i]).canSee)
			{
				float num2 = Vector2.Distance(refPos, players[i].data.playerVel.position);
				num2 += Vector2.Angle(forward, players[i].data.playerVel.position - refPos);
				if (num2 < num)
				{
					num = num2;
					result = players[i];
				}
			}
		}
		return result;
	}

	public CanSeeInfo CanSeePlayer(Vector2 from, Player player)
	{
		CanSeeInfo canSeeInfo = new CanSeeInfo();
		canSeeInfo.canSee = true;
		canSeeInfo.distance = float.PositiveInfinity;
		if (!player)
		{
			canSeeInfo.canSee = false;
			return canSeeInfo;
		}
		RaycastHit2D[] array = Physics2D.RaycastAll(from, (player.data.playerVel.position - from).normalized, Vector2.Distance(from, player.data.playerVel.position), canSeePlayerMask);
		for (int i = 0; i < array.Length; i++)
		{
			if ((bool)array[i].transform && !array[i].transform.root.GetComponent<SpawnedAttack>() && !array[i].transform.root.GetComponent<Player>() && array[i].distance < canSeeInfo.distance)
			{
				canSeeInfo.canSee = false;
				canSeeInfo.hitPoint = array[i].point;
				canSeeInfo.distance = array[i].distance;
			}
		}
		return canSeeInfo;
	}

	internal Player GetPlayerWithID(int playerID)
	{
		for (int i = 0; i < players.Count; i++)
		{
			if (players[i].playerID == playerID)
			{
				return players[i];
			}
		}
		return null;
	}

	public Player GetLastPlayerAlive()
	{
		Player result = null;
		for (int i = 0; i < players.Count; i++)
		{
			if (!players[i].data.dead)
			{
				result = players[i];
				break;
			}
		}
		return result;
	}

	public int GetLastTeamAlive()
	{
		return GetLastPlayerAlive().teamID;
	}

	public int TeamsAlive()
	{
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < players.Count; i++)
		{
			if (players[i].teamID == 0 && !players[i].data.dead)
			{
				flag = true;
			}
			if (players[i].teamID == 1 && !players[i].data.dead)
			{
				flag2 = true;
			}
		}
		int num = 0;
		if (flag)
		{
			num++;
		}
		if (flag2)
		{
			num++;
		}
		return num;
	}

	public static void RegisterPlayer(Player player)
	{
		instance.players.Add(player);
		if (instance.playersShouldBeActive)
		{
			player.data.isPlaying = true;
		}
	}

	public void RemovePlayer(Player player)
	{
		players.Remove(player);
		UnityEngine.Object.Destroy(player.gameObject);
	}

	public void RemovePlayers()
	{
		for (int num = players.Count - 1; num >= 0; num--)
		{
			if ((bool)players[num])
			{
				UnityEngine.Object.Destroy(players[num].gameObject);
			}
		}
		players.Clear();
		PlayerAssigner.instance.ClearPlayers();
	}

	public void RevivePlayers()
	{
		for (int i = 0; i < players.Count; i++)
		{
			players[i].data.healthHandler.Revive();
			players[i].GetComponent<GeneralInput>().enabled = true;
		}
	}

	[PunRPC]
	public void RPCA_MovePlayers()
	{
		MovePlayers(MapManager.instance.GetSpawnPoints());
	}

	public void MovePlayers(SpawnPoint[] spawnPoints)
	{
		for (int i = 0; i < players.Count; i++)
		{
			StartCoroutine(Move(players[i].data.playerVel, spawnPoints[i].localStartPos));
			int num;
			for (num = i; num >= soundCharacterSpawn.Length; num -= soundCharacterSpawn.Length)
			{
			}
			SoundManager.Instance.Play(soundCharacterSpawn[num], players[i].transform);
		}
	}

	public void AddPlayerDiedAction(Action<Player, int> action)
	{
		PlayerDiedAction = (Action<Player, int>)Delegate.Combine(PlayerDiedAction, action);
	}

	public void PlayerDied(Player player)
	{
		int num = 0;
		for (int i = 0; i < players.Count; i++)
		{
			if (!instance.players[i].data.dead)
			{
				num++;
			}
		}
		if (PlayerDiedAction != null)
		{
			PlayerDiedAction(player, num);
		}
	}

	public PlayerSkin GetColorFromTeam(int teamID)
	{
		return PlayerSkinBank.GetPlayerSkinColors(GetPlayersInTeam(teamID)[0].playerID);
	}

	public PlayerSkin GetColorFromPlayer(int playerID)
	{
		return PlayerSkinBank.GetPlayerSkinColors(playerID);
	}

	public Player[] GetPlayersInTeam(int teamID)
	{
		List<Player> list = new List<Player>();
		for (int i = 0; i < players.Count; i++)
		{
			if (players[i].teamID == teamID)
			{
				list.Add(players[i]);
			}
		}
		return list.ToArray();
	}

	internal Player GetFirstPlayerInTeam(int teamID)
	{
		return GetPlayersInTeam(teamID)[0];
	}

	public int GetOtherTeam(int team)
	{
		if (team == 0)
		{
			return 1;
		}
		return 0;
	}

	public void SetPlayersPlaying(bool playing)
	{
		playersShouldBeActive = playing;
		for (int i = 0; i < players.Count; i++)
		{
			players[i].data.isPlaying = playing;
		}
	}

	public void SetPlayersSimulated(bool simulated)
	{
		playersShouldBeActive = simulated;
		for (int i = 0; i < players.Count; i++)
		{
			players[i].data.playerVel.simulated = simulated;
		}
	}

	internal void SetPlayersVisible(bool visible)
	{
		for (int i = 0; i < players.Count; i++)
		{
			players[i].data.gameObject.transform.position = Vector3.up * 200f;
		}
	}

	public void PlayerJoined(Player player)
	{
		if (PlayerJoinedAction != null)
		{
			PlayerJoinedAction(player);
		}
	}

	private IEnumerator Move(PlayerVelocity player, Vector3 targetPos)
	{
		Debug.Log("MOVE PLAYERS START " + Time.unscaledTime);
		player.GetComponent<Player>().data.isPlaying = false;
		player.simulated = false;
		player.isKinematic = true;
		Vector3 distance = targetPos - player.transform.position;
		Vector3 targetStartPos = player.transform.position;
		PlayerCollision col = player.GetComponent<PlayerCollision>();
		float t = playerMoveCurve.keys[playerMoveCurve.keys.Length - 1].time;
		float c = 0f;
		while (c < t)
		{
			col.IgnoreWallForFrames(2);
			c += Mathf.Clamp(Time.unscaledDeltaTime, 0f, 0.02f);
			player.transform.position = targetStartPos + distance * playerMoveCurve.Evaluate(c);
			yield return null;
		}
		int frames = 0;
		while (frames < 10)
		{
			player.transform.position = targetStartPos + distance;
			frames++;
			yield return null;
		}
		player.simulated = true;
		player.isKinematic = false;
		Debug.Log("MOVE PLAYERS END " + Time.unscaledTime);
		player.GetComponent<Player>().data.isPlaying = true;
		player.GetComponent<Player>().data.healthHandler.Revive();
		CardChoiceVisuals.instance.Hide();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.R) && !DevConsole.isTyping && Application.isEditor)
		{
			ResetCharacters();
		}
	}

	internal void ResetCharacters()
	{
		CardBarHandler.instance.ResetCardBards();
		for (int i = 0; i < players.Count; i++)
		{
			players[i].FullReset();
		}
	}

	public PlayerActions[] GetActionsFromTeam(int selectingTeamID)
	{
		List<PlayerActions> list = new List<PlayerActions>();
		for (int i = 0; i < players.Count; i++)
		{
			if (players[i].teamID == selectingTeamID)
			{
				list.Add(players[i].data.playerActions);
			}
		}
		return list.ToArray();
	}

	public PlayerActions[] GetActionsFromPlayer(int selectingPlayerID)
	{
		List<PlayerActions> list = new List<PlayerActions>();
		for (int i = 0; i < players.Count; i++)
		{
			if (players[i].playerID == selectingPlayerID)
			{
				list.Add(players[i].data.playerActions);
			}
		}
		return list.ToArray();
	}
}
