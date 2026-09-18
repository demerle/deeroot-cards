using System;
using System.Collections;
using System.Runtime.CompilerServices;
using ExitGames.Client.Photon;
using Landfall.Network;
using Photon.Pun;
using Photon.Realtime;
using SoundImplementation;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkConnectionHandler : MonoBehaviourPunCallbacks
{
	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass10_0
	{
		public NetworkConnectionHandler _003C_003E4__this;

		public RoomOptions options;

		internal void _003CHostPrivateAndInviteFriend_003Eb__0()
		{
			_003C_003E4__this.CreateRoom(options);
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass22_0
	{
		public NetworkConnectionHandler _003C_003E4__this;

		public string room;

		internal void _003CForceRegionJoin_003Eb__0()
		{
			_003C_003E4__this.JoinSpecificRoom(room);
		}
	}

	[CompilerGenerated]
	private sealed class _003C_003Ec__DisplayClass29_0
	{
		public RoomOptions roomOptions;

		internal void _003CCreateRoom_003Eb__0(string RoomName)
		{
			PhotonNetwork.CreateRoom(RoomName, roomOptions);
		}
	}

	public static readonly string TWITCH_PLAYER_SCORE_KEY = "TwitchScore";

	public static readonly string TWITCH_ROOM_AUDIENCE_RATING_KEY = "C0";

	public static NetworkConnectionHandler instance;

	private static ClientSteamLobby m_SteamLobby;

	private bool m_SearchingQuickMatch;

	private bool m_SearchingTwitch;

	private int currentViewers = 100;

	private TypedLobby sqlLobby = new TypedLobby("customSqlLobby", LobbyType.SqlLobby);

	public bool hasRegionSelect;

	private bool m_ForceRegion;

	private bool isConnectedToMaster;

	private float untilTryOtherRegionCounter;

	private void Start()
	{
		instance = this;
		PhotonNetwork.ServerPortOverrides = PhotonPortDefinition.AlternativeUdpPorts;
		PhotonNetwork.CrcCheckEnabled = true;
		PhotonNetwork.NetworkingClient.LoadBalancingPeer.DisconnectTimeout = 30000;
		if (m_SteamLobby == null)
		{
			m_SteamLobby = new ClientSteamLobby();
		}
		else
		{
			m_SteamLobby.LeaveLobby();
		}
	}

	private void Update()
	{
		if (m_SearchingQuickMatch && PhotonNetwork.InRoom && !PhotonNetwork.OfflineMode && !GM_ArmsRace.instance)
		{
			untilTryOtherRegionCounter -= Time.deltaTime;
			if (untilTryOtherRegionCounter < 0f)
			{
				StartCoroutine(PlayOnBestActiveRegion());
			}
		}
	}

	public void QuickMatch()
	{
		m_SearchingQuickMatch = true;
		m_SearchingTwitch = false;
		TimeHandler.instance.gameStartTime = 1f;
		LoadingScreen loadingScreen = LoadingScreen.instance;
		if ((object)loadingScreen != null)
		{
			loadingScreen.StartLoading();
		}
		StartCoroutine(DoActionWhenConnected(JoinRandomRoom));
	}

	public void TwitchJoin(int score)
	{
		currentViewers = Mathf.Clamp(score, 1, score);
		m_SearchingQuickMatch = false;
		m_SearchingTwitch = true;
		ExitGames.Client.Photon.Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
		if (customProperties.ContainsKey(TWITCH_PLAYER_SCORE_KEY))
		{
			customProperties[TWITCH_PLAYER_SCORE_KEY] = score;
		}
		else
		{
			customProperties.Add(TWITCH_PLAYER_SCORE_KEY, score);
		}
		PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);
		TimeHandler.instance.gameStartTime = 1f;
		LoadingScreen loadingScreen = LoadingScreen.instance;
		if ((object)loadingScreen != null)
		{
			loadingScreen.StartLoading();
		}
		StartCoroutine(DoActionWhenConnected(JoinSpecificTWITCHRoom));
	}

	public void HostPrivateAndInviteFriend()
	{
		_003C_003Ec__DisplayClass10_0 _003C_003Ec__DisplayClass10_ = new _003C_003Ec__DisplayClass10_0();
		_003C_003Ec__DisplayClass10_._003C_003E4__this = this;
		m_SearchingQuickMatch = false;
		m_SearchingTwitch = false;
		TimeHandler.instance.gameStartTime = 1f;
		LoadingScreen loadingScreen = LoadingScreen.instance;
		if ((object)loadingScreen != null)
		{
			loadingScreen.StartLoading(true);
		}
		_003C_003Ec__DisplayClass10_.options = new RoomOptions();
		_003C_003Ec__DisplayClass10_.options.MaxPlayers = 2;
		_003C_003Ec__DisplayClass10_.options.IsOpen = true;
		_003C_003Ec__DisplayClass10_.options.IsVisible = false;
		m_SteamLobby.ShowInviteScreenWhenConnected();
		StartCoroutine(DoActionWhenConnected(_003C_003Ec__DisplayClass10_._003CHostPrivateAndInviteFriend_003Eb__0));
	}

	private void JoinRandomRoom()
	{
		Debug.Log("Joining random room");
		PhotonNetwork.JoinRandomRoom();
	}

	private void CreateSpecificTWITCHRoom()
	{
		Debug.Log("Creating SPECIFIC TWITCH ROOM!");
		RoomOptions roomOptions = new RoomOptions();
		roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { TWITCH_ROOM_AUDIENCE_RATING_KEY, currentViewers } };
		roomOptions.CustomRoomPropertiesForLobby = new string[1] { TWITCH_ROOM_AUDIENCE_RATING_KEY };
		PhotonNetwork.CreateRoom(null, roomOptions, sqlLobby);
	}

	private void JoinSpecificTWITCHRoom()
	{
		Debug.Log("JOINING SPECIFIC TWITCH ROOM!");
		int num = 5;
		int num2 = currentViewers * num;
		int num3 = currentViewers / num;
		int num4 = 10;
		int num5 = currentViewers * num4;
		int num6 = currentViewers / num4;
		int num7 = 10000000;
		int num8 = currentViewers * num7;
		int num9 = currentViewers / num7;
		string text = "";
		text = text + "C0 BETWEEN " + num3 + " AND " + num2 + ";";
		text = text + "C0 BETWEEN " + num6 + " AND " + num5 + ";";
		text = text + "C0 BETWEEN " + num9 + " AND " + num8;
		PhotonNetwork.JoinRandomRoom(null, 0, MatchmakingMode.FillRoom, sqlLobby, text);
	}

	public override void OnJoinRoomFailed(short returnCode, string message)
	{
		Debug.Log("JOINED RANDOM ROOM FAILED!");
		if (!m_SearchingTwitch)
		{
			JoinRandomRoom();
		}
		else
		{
			CreateSpecificTWITCHRoom();
		}
	}

	private IEnumerator DoActionWhenConnected(Action action)
	{
		yield return WaitForConnect();
		action();
	}

	private IEnumerator PlayOnBestActiveRegion()
	{
		if (PhotonNetwork.InRoom)
		{
			PhotonNetwork.LeaveRoom();
			while (PhotonNetwork.InRoom)
			{
				yield return null;
			}
		}
		string[] regionsToTry = new string[13]
		{
			"usw", "eu", "us", "au", "ru", "za", "asia", "cae", "in", "jp",
			"rue", "sa", "kr"
		};
		float bestRegionScore = 0f;
		string bestRegion = "";
		for (int i = 0; i < regionsToTry.Length; i++)
		{
			isConnectedToMaster = false;
			PhotonNetwork.Disconnect();
			while (PhotonNetwork.IsConnected)
			{
				yield return null;
			}
			PhotonNetwork.ConnectToRegion(regionsToTry[i]);
			Debug.Log("connectToRegion " + regionsToTry[i]);
			isConnectedToMaster = false;
			while (!isConnectedToMaster)
			{
				yield return null;
			}
			int countOfPlayersInRooms = PhotonNetwork.CountOfPlayersInRooms;
			int ping = PhotonNetwork.GetPing();
			float num = (float)Mathf.Clamp(countOfPlayersInRooms, 0, 50) / Mathf.Clamp(ping, 10f, 1E+11f);
			Debug.Log("Ping: " + Mathf.Clamp(PhotonNetwork.GetPing(), 10f, 1E+11f));
			Debug.Log(regionsToTry[i] + ": " + PhotonNetwork.CountOfPlayersInRooms);
			if (num > bestRegionScore)
			{
				bestRegion = regionsToTry[i];
				bestRegionScore = num;
				if (ping < 50 && countOfPlayersInRooms > 50)
				{
					break;
				}
			}
		}
		isConnectedToMaster = false;
		PhotonNetwork.Disconnect();
		PhotonNetwork.LocalPlayer.NickName = "PlayerName";
		if (bestRegion == "")
		{
			PhotonNetwork.ConnectToBestCloudServer();
		}
		else
		{
			Debug.Log("Connecting to " + bestRegion);
			PhotonNetwork.ConnectToRegion(bestRegion);
		}
		while (!isConnectedToMaster)
		{
			yield return null;
		}
		JoinRandomRoom();
	}

	private IEnumerator WaitForConnect()
	{
		if (!PhotonNetwork.IsConnectedAndReady)
		{
			PhotonNetwork.LocalPlayer.NickName = "PlayerName";
			PhotonNetwork.ConnectUsingSettings();
			if (hasRegionSelect || m_ForceRegion)
			{
				PhotonNetwork.ConnectToRegion(RegionSelector.region);
			}
			else
			{
				PhotonNetwork.ConnectToBestCloudServer();
			}
		}
		while (!isConnectedToMaster)
		{
			Debug.Log("Trying to connect to photon");
			yield return null;
		}
		Debug.Log("Is connected");
	}

	public void ForceRegionJoin(string region, string room)
	{
		_003C_003Ec__DisplayClass22_0 _003C_003Ec__DisplayClass22_ = new _003C_003Ec__DisplayClass22_0();
		_003C_003Ec__DisplayClass22_._003C_003E4__this = this;
		_003C_003Ec__DisplayClass22_.room = room;
		Debug.Log("CREEASDSSD");
		if (PhotonNetwork.InRoom)
		{
			PhotonNetwork.Disconnect();
		}
		CharacterCreatorHandler.instance.CloseMenus();
		MainMenuHandler.instance.Close();
		RegionSelector.region = region;
		TimeHandler.instance.gameStartTime = 1f;
		LoadingScreen loadingScreen = LoadingScreen.instance;
		if ((object)loadingScreen != null)
		{
			loadingScreen.StartLoading();
		}
		m_ForceRegion = true;
		StartCoroutine(DoActionWhenConnected(_003C_003Ec__DisplayClass22_._003CForceRegionJoin_003Eb__0));
	}

	private void JoinSpecificRoom(string room)
	{
		PhotonNetwork.JoinRoom(room);
		m_ForceRegion = false;
	}

	public override void OnEnable()
	{
		base.OnEnable();
		Debug.Log("Add me!");
		PhotonNetwork.AddCallbackTarget(this);
	}

	public override void OnDisable()
	{
		base.OnDisable();
		Debug.Log("Remove me!");
		PhotonNetwork.RemoveCallbackTarget(this);
	}

	public override void OnConnectedToMaster()
	{
		isConnectedToMaster = true;
	}

	public override void OnJoinRandomFailed(short returnCode, string message)
	{
		Debug.Log("JOINED RANDOM ROOM FAILED!");
		if (m_SearchingTwitch)
		{
			CreateSpecificTWITCHRoom();
			return;
		}
		RoomOptions roomOptions = new RoomOptions();
		roomOptions.MaxPlayers = 2;
		roomOptions.IsOpen = true;
		roomOptions.IsVisible = true;
		if (!SteamManager.Initialized)
		{
			Debug.LogError("SteamManager is not initialized!");
		}
		else
		{
			CreateRoom(roomOptions);
		}
	}

	private void CreateRoom(RoomOptions roomOptions)
	{
		_003C_003Ec__DisplayClass29_0 _003C_003Ec__DisplayClass29_ = new _003C_003Ec__DisplayClass29_0();
		_003C_003Ec__DisplayClass29_.roomOptions = roomOptions;
		m_SteamLobby.CreateLobby(_003C_003Ec__DisplayClass29_.roomOptions.MaxPlayers, _003C_003Ec__DisplayClass29_._003CCreateRoom_003Eb__0);
	}

	public override void OnJoinedRoom()
	{
		if (!PhotonNetwork.OfflineMode)
		{
			isConnectedToMaster = false;
			Debug.Log("Room joined successfully");
			Debug.Log(PhotonNetwork.CloudRegion);
			untilTryOtherRegionCounter = 15f;
			PhotonNetwork.LocalPlayer.NickName = (m_SearchingTwitch ? TwitchUIHandler.TWITCH_NAME_KEY : SteamFriends.GetPersonaName());
		}
	}

	public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
	{
		SoundPlayerStatic.Instance.PlayPlayerAdded();
		if (PhotonNetwork.PlayerList.Length == 2)
		{
			if (PhotonNetwork.IsMasterClient)
			{
				GetComponent<PhotonView>().RPC("RPCA_FoundGame", RpcTarget.All);
			}
			if (m_SteamLobby != null)
			{
				m_SteamLobby.HideLobby();
			}
		}
		Debug.Log("PlayerJoined");
		base.OnPlayerEnteredRoom(newPlayer);
	}

	[PunRPC]
	private void RPCA_FoundGame()
	{
		LoadingScreen loadingScreen = LoadingScreen.instance;
		if ((object)loadingScreen != null)
		{
			loadingScreen.StopLoading();
		}
	}

	public override void OnLeftRoom()
	{
		isConnectedToMaster = false;
	}

	public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
	{
		bool flag = GM_ArmsRace.instance == null;
		StartCoroutine(DoDisconnect("DISCONNECTED", "Other player left"));
		base.OnPlayerLeftRoom(otherPlayer);
	}

	public override void OnDisconnected(DisconnectCause cause)
	{
		if (cause != 0 && cause != DisconnectCause.DisconnectByClientLogic)
		{
			StartCoroutine(DoDisconnect("DISCONNECTED", cause.ToString()));
			isConnectedToMaster = false;
		}
	}

	private IEnumerator DoRetry()
	{
		LoadingScreen.instance.StartLoading();
		if (PhotonNetwork.InRoom)
		{
			PhotonNetwork.LeaveRoom();
			while (PhotonNetwork.InRoom)
			{
				yield return null;
			}
		}
		JoinRandomRoom();
	}

	private IEnumerator DoDisconnect(string context, string reason)
	{
		ErrorHandler.instance.ShowError(context, reason);
		yield return new WaitForSecondsRealtime(2f);
		ErrorHandler.instance.HideError();
		NetworkRestart();
	}

	public override void OnRegionListReceived(RegionHandler regionHandler)
	{
		Debug.Log(regionHandler);
	}

	public void NetworkRestart()
	{
		isConnectedToMaster = false;
		if (PhotonNetwork.OfflineMode)
		{
			Application.LoadLevel(Application.loadedLevel);
		}
		else
		{
			StartCoroutine(WaitForRestart());
		}
	}

	private IEnumerator WaitForRestart()
	{
		if (m_SteamLobby != null)
		{
			m_SteamLobby.LeaveLobby();
		}
		if (PhotonNetwork.InRoom)
		{
			PhotonNetwork.LeaveRoom();
			while (PhotonNetwork.InRoom)
			{
				yield return null;
			}
		}
		if (PhotonNetwork.IsConnected)
		{
			PhotonNetwork.Disconnect();
			while (PhotonNetwork.IsConnected)
			{
				yield return null;
			}
		}
		EscapeMenuHandler.isEscMenu = false;
		DevConsole.isTyping = false;
		Application.LoadLevel(Application.loadedLevel);
	}
}
