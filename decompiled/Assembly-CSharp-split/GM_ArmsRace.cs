using System;
using System.Collections;
using Photon.Pun;
using UnityEngine;

public class GM_ArmsRace : MonoBehaviour
{
	private int playersNeededToStart = 2;

	private int pointsToWinRound = 2;

	public int roundsToWinGame = 5;

	public int p1Points;

	public int p2Points;

	public int p1Rounds;

	public int p2Rounds;

	private PhotonView view;

	public static GM_ArmsRace instance;

	private bool isWaiting;

	public Action StartGameAction;

	public bool pickPhase = true;

	[HideInInspector]
	public bool isPicking;

	private bool waitingForOtherPlayer = true;

	private int currentWinningTeamID = -1;

	public Action pointOverAction;

	private bool isTransitioning;

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		view = GetComponent<PhotonView>();
		PlayerManager.instance.SetPlayersSimulated(simulated: false);
		PlayerAssigner.instance.maxPlayers = playersNeededToStart;
		PlayerAssigner.instance.SetPlayersCanJoin(canJoin: true);
		PlayerManager.instance.AddPlayerDiedAction(PlayerDied);
		PlayerManager playerManager = PlayerManager.instance;
		playerManager.PlayerJoinedAction = (Action<Player>)Delegate.Combine(playerManager.PlayerJoinedAction, new Action<Player>(PlayerJoined));
		ArtHandler.instance.NextArt();
		playersNeededToStart = 2;
		UIHandler.instance.SetNumberOfRounds(roundsToWinGame);
		PlayerAssigner.instance.maxPlayers = playersNeededToStart;
		if (!PhotonNetwork.OfflineMode)
		{
			UIHandler.instance.ShowJoinGameText("PRESS JUMP\n TO JOIN", PlayerSkinBank.GetPlayerSkinColors(0).winText);
		}
	}

	private void Update()
	{
		if (Input.GetKey(KeyCode.Alpha4))
		{
			playersNeededToStart = 4;
			PlayerAssigner.instance.maxPlayers = playersNeededToStart;
		}
		if (Input.GetKey(KeyCode.Alpha2))
		{
			playersNeededToStart = 2;
			PlayerAssigner.instance.maxPlayers = playersNeededToStart;
		}
	}

	public void PlayerJoined(Player player)
	{
		if (PhotonNetwork.OfflineMode)
		{
			return;
		}
		if (!PhotonNetwork.OfflineMode)
		{
			if (player.data.view.IsMine)
			{
				UIHandler.instance.ShowJoinGameText("WAITING", PlayerSkinBank.GetPlayerSkinColors(1).winText);
			}
			else
			{
				UIHandler.instance.ShowJoinGameText("PRESS JUMP\n TO JOIN", PlayerSkinBank.GetPlayerSkinColors(1).winText);
			}
		}
		player.data.isPlaying = false;
		int count = PlayerManager.instance.players.Count;
		if (count >= playersNeededToStart)
		{
			StartGame();
		}
		else if (PhotonNetwork.OfflineMode)
		{
			if (playersNeededToStart - count == 3)
			{
				UIHandler.instance.ShowJoinGameText("ADD THREE MORE PLAYER TO START", PlayerSkinBank.GetPlayerSkinColors(count).winText);
			}
			if (playersNeededToStart - count == 2)
			{
				UIHandler.instance.ShowJoinGameText("ADD TWO MORE PLAYER TO START", PlayerSkinBank.GetPlayerSkinColors(count).winText);
			}
			if (playersNeededToStart - count == 1)
			{
				UIHandler.instance.ShowJoinGameText("ADD ONE MORE PLAYER TO START", PlayerSkinBank.GetPlayerSkinColors(count).winText);
			}
		}
	}

	[PunRPC]
	private void RPCO_RequestSyncUp()
	{
		view.RPC("RPCM_ReturnSyncUp", RpcTarget.Others);
	}

	[PunRPC]
	private void RPCM_ReturnSyncUp()
	{
		isWaiting = false;
	}

	private IEnumerator WaitForSyncUp()
	{
		if (!PhotonNetwork.OfflineMode)
		{
			isWaiting = true;
			view.RPC("RPCO_RequestSyncUp", RpcTarget.Others);
			while (isWaiting)
			{
				yield return null;
			}
		}
	}

	public void StartGame()
	{
		if (!GameManager.instance.isPlaying)
		{
			StartGameAction?.Invoke();
			GameManager.instance.isPlaying = true;
			StartCoroutine(DoStartGame());
		}
	}

	private IEnumerator DoStartGame()
	{
		GameManager.instance.battleOngoing = false;
		UIHandler.instance.ShowJoinGameText("LETS GOO!", PlayerSkinBank.GetPlayerSkinColors(1).winText);
		yield return new WaitForSeconds(0.25f);
		UIHandler.instance.HideJoinGameText();
		PlayerManager.instance.SetPlayersSimulated(simulated: false);
		PlayerManager.instance.SetPlayersVisible(visible: false);
		MapManager.instance.LoadNextLevel();
		TimeHandler.instance.DoSpeedUp();
		yield return new WaitForSecondsRealtime(1f);
		if (pickPhase)
		{
			for (int i = 0; i < PlayerManager.instance.players.Count; i++)
			{
				yield return StartCoroutine(WaitForSyncUp());
				CardChoiceVisuals.instance.Show(i, animateIn: true);
				yield return CardChoice.instance.DoPick(1, PlayerManager.instance.players[i].playerID, PickerType.Player);
				yield return new WaitForSecondsRealtime(0.1f);
			}
			yield return StartCoroutine(WaitForSyncUp());
			CardChoiceVisuals.instance.Hide();
		}
		MapManager.instance.CallInNewMapAndMovePlayers(MapManager.instance.currentLevelID);
		TimeHandler.instance.DoSpeedUp();
		TimeHandler.instance.StartGame();
		GameManager.instance.battleOngoing = true;
		UIHandler.instance.ShowRoundCounterSmall(p1Rounds, p2Rounds, p1Points, p2Points);
		PlayerManager.instance.SetPlayersVisible(visible: true);
	}

	private IEnumerator PointTransition(int winningTeamID, string winTextBefore, string winText)
	{
		StartCoroutine(PointVisualizer.instance.DoSequence(p1Points, p2Points, winningTeamID == 0));
		yield return new WaitForSecondsRealtime(1f);
		MapManager.instance.LoadNextLevel();
		yield return new WaitForSecondsRealtime(0.5f);
		yield return StartCoroutine(WaitForSyncUp());
		MapManager.instance.CallInNewMapAndMovePlayers(MapManager.instance.currentLevelID);
		PlayerManager.instance.RevivePlayers();
		yield return new WaitForSecondsRealtime(0.3f);
		TimeHandler.instance.DoSpeedUp();
		GameManager.instance.battleOngoing = true;
		isTransitioning = false;
	}

	private void PointOver(int winningTeamID)
	{
		int num = p1Points;
		int num2 = p2Points;
		if (winningTeamID == 0)
		{
			num--;
		}
		else
		{
			num2--;
		}
		string winTextBefore = num + " - " + num2;
		string winText = p1Points + " - " + p2Points;
		StartCoroutine(PointTransition(winningTeamID, winTextBefore, winText));
		UIHandler.instance.ShowRoundCounterSmall(p1Rounds, p2Rounds, p1Points, p2Points);
	}

	private IEnumerator RoundTransition(int winningTeamID, int killedTeamID)
	{
		StartCoroutine(PointVisualizer.instance.DoWinSequence(p1Points, p2Points, p1Rounds, p2Rounds, winningTeamID == 0));
		yield return new WaitForSecondsRealtime(1f);
		MapManager.instance.LoadNextLevel();
		yield return new WaitForSecondsRealtime(0.3f);
		yield return new WaitForSecondsRealtime(1f);
		TimeHandler.instance.DoSpeedUp();
		if (pickPhase)
		{
			Debug.Log("PICK PHASE");
			PlayerManager.instance.SetPlayersVisible(visible: false);
			Player[] players = PlayerManager.instance.GetPlayersInTeam(killedTeamID);
			for (int i = 0; i < players.Length; i++)
			{
				yield return StartCoroutine(WaitForSyncUp());
				yield return CardChoice.instance.DoPick(1, players[i].playerID, PickerType.Player);
				yield return new WaitForSecondsRealtime(0.1f);
			}
			PlayerManager.instance.SetPlayersVisible(visible: true);
		}
		yield return StartCoroutine(WaitForSyncUp());
		TimeHandler.instance.DoSlowDown();
		MapManager.instance.CallInNewMapAndMovePlayers(MapManager.instance.currentLevelID);
		PlayerManager.instance.RevivePlayers();
		yield return new WaitForSecondsRealtime(0.3f);
		TimeHandler.instance.DoSpeedUp();
		isTransitioning = false;
		GameManager.instance.battleOngoing = true;
		UIHandler.instance.ShowRoundCounterSmall(p1Rounds, p2Rounds, p1Points, p2Points);
	}

	private void RoundOver(int winningTeamID, int losingTeamID)
	{
		currentWinningTeamID = winningTeamID;
		StartCoroutine(RoundTransition(winningTeamID, losingTeamID));
		p1Points = 0;
		p2Points = 0;
	}

	private IEnumerator GameOverTransition(int winningTeamID)
	{
		UIHandler.instance.ShowRoundCounterSmall(p1Rounds, p2Rounds, p1Points, p2Points);
		UIHandler.instance.DisplayScreenText(PlayerManager.instance.GetColorFromTeam(winningTeamID).winText, "VICTORY!", 1f);
		yield return new WaitForSecondsRealtime(2f);
		GameOverRematch(winningTeamID);
	}

	private void GameOverRematch(int winningTeamID)
	{
		UIHandler.instance.DisplayScreenTextLoop(PlayerManager.instance.GetColorFromTeam(winningTeamID).winText, "REMATCH?");
		UIHandler.instance.DisplayYesNoLoop(PlayerManager.instance.GetFirstPlayerInTeam(winningTeamID), GetRematchYesNo);
		MapManager.instance.LoadNextLevel();
	}

	private void GetRematchYesNo(PopUpHandler.YesNo yesNo)
	{
		if (yesNo == PopUpHandler.YesNo.Yes)
		{
			StartCoroutine(IDoRematch());
		}
		else
		{
			DoRestart();
		}
	}

	[PunRPC]
	public void RPCA_PlayAgain()
	{
		waitingForOtherPlayer = false;
	}

	private IEnumerator IDoRematch()
	{
		if (!PhotonNetwork.OfflineMode)
		{
			GetComponent<PhotonView>().RPC("RPCA_PlayAgain", RpcTarget.Others);
			UIHandler.instance.DisplayScreenTextLoop("WAITING");
			float c = 0f;
			while (waitingForOtherPlayer)
			{
				c += Time.unscaledDeltaTime;
				if (c > 10f)
				{
					DoRestart();
					yield break;
				}
				yield return null;
			}
		}
		yield return null;
		UIHandler.instance.StopScreenTextLoop();
		PlayerManager.instance.ResetCharacters();
		ResetMatch();
		StartCoroutine(DoStartGame());
		waitingForOtherPlayer = true;
	}

	private void ResetMatch()
	{
		p1Points = 0;
		p1Rounds = 0;
		p2Points = 0;
		p2Rounds = 0;
		isTransitioning = false;
		waitingForOtherPlayer = false;
		UIHandler.instance.ShowRoundCounterSmall(p1Rounds, p2Rounds, p1Points, p2Points);
		CardBarHandler.instance.ResetCardBards();
		PointVisualizer.instance.ResetPoints();
	}

	private void GameOverContinue(int winningTeamID)
	{
		UIHandler.instance.DisplayScreenTextLoop(PlayerManager.instance.GetColorFromTeam(winningTeamID).winText, "CONTINUE?");
		UIHandler.instance.DisplayYesNoLoop(PlayerManager.instance.GetFirstPlayerInTeam(winningTeamID), GetContinueYesNo);
		MapManager.instance.LoadNextLevel();
	}

	private void GetContinueYesNo(PopUpHandler.YesNo yesNo)
	{
		if (yesNo == PopUpHandler.YesNo.Yes)
		{
			DoContinue();
		}
		else
		{
			DoRestart();
		}
	}

	private void DoContinue()
	{
		UIHandler.instance.StopScreenTextLoop();
		roundsToWinGame += 2;
		UIHandler.instance.SetNumberOfRounds(roundsToWinGame);
		RoundOver(currentWinningTeamID, PlayerManager.instance.GetOtherTeam(currentWinningTeamID));
	}

	private void DoRestart()
	{
		GameManager.instance.battleOngoing = false;
		if (PhotonNetwork.OfflineMode)
		{
			Application.LoadLevel(Application.loadedLevel);
		}
		else
		{
			NetworkConnectionHandler.instance.NetworkRestart();
		}
	}

	private void GameOver(int winningTeamID)
	{
		currentWinningTeamID = winningTeamID;
		StartCoroutine(GameOverTransition(winningTeamID));
	}

	public void PlayerDied(Player killedPlayer, int playersAlive)
	{
		if (!PhotonNetwork.OfflineMode)
		{
			Debug.Log("PlayerDied: " + killedPlayer.data.view.Owner.NickName);
		}
		if (PlayerManager.instance.TeamsAlive() < 2)
		{
			TimeHandler.instance.DoSlowDown();
			if (PhotonNetwork.IsMasterClient)
			{
				view.RPC("RPCA_NextRound", RpcTarget.All, PlayerManager.instance.GetOtherTeam(PlayerManager.instance.GetLastTeamAlive()), PlayerManager.instance.GetLastTeamAlive(), p1Points, p2Points, p1Rounds, p2Rounds);
			}
		}
	}

	[PunRPC]
	public void RPCA_NextRound(int losingTeamID, int winningTeamID, int p1PointsSet, int p2PointsSet, int p1RoundsSet, int p2RoundsSet)
	{
		if (isTransitioning)
		{
			return;
		}
		GameManager.instance.battleOngoing = false;
		p1Points = p1PointsSet;
		p2Points = p2PointsSet;
		p1Rounds = p1RoundsSet;
		p2Rounds = p2RoundsSet;
		Debug.Log("Winning team: " + winningTeamID);
		Debug.Log("Losing team: " + losingTeamID);
		isTransitioning = true;
		GameManager.instance.GameOver(winningTeamID, losingTeamID);
		PlayerManager.instance.SetPlayersSimulated(simulated: false);
		switch (winningTeamID)
		{
		case 0:
			p1Points++;
			if (p1Points >= pointsToWinRound)
			{
				p1Rounds++;
				if (p1Rounds >= roundsToWinGame)
				{
					Debug.Log("Game over, winning team: " + winningTeamID);
					GameOver(winningTeamID);
					pointOverAction();
				}
				else
				{
					Debug.Log("Round over, winning team: " + winningTeamID);
					RoundOver(winningTeamID, losingTeamID);
					pointOverAction();
				}
			}
			else
			{
				Debug.Log("Point over, winning team: " + winningTeamID);
				PointOver(winningTeamID);
				pointOverAction();
			}
			break;
		case 1:
			p2Points++;
			if (p2Points >= pointsToWinRound)
			{
				p2Rounds++;
				if (p2Rounds >= roundsToWinGame)
				{
					Debug.Log("Game over, winning team: " + winningTeamID);
					GameOver(winningTeamID);
					pointOverAction();
				}
				else
				{
					Debug.Log("Round over, winning team: " + winningTeamID);
					RoundOver(winningTeamID, losingTeamID);
					pointOverAction();
				}
			}
			else
			{
				Debug.Log("Point over, winning team: " + winningTeamID);
				PointOver(winningTeamID);
				pointOverAction();
			}
			break;
		}
	}
}
