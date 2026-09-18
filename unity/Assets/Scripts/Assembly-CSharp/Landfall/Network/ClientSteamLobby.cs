using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Photon.Pun;
using Steamworks;

namespace Landfall.Network
{
	public class ClientSteamLobby
	{
		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass57_0
		{
			public ClientSteamLobby _003C_003E4__this;

			public LobbyInvite_t param;

			internal void _003COnInviteToLobby_003Eb__0()
			{
				_003C_003E4__this.LeaveCurrentLobby();
				_003C_003E4__this.JoinLobby(new CSteamID(param.m_ulSteamIDLobby));
			}
		}

		private const string REGION_KEY = "RegionKey";

		private const string ROOM_KEY = "RoomKey";

		private const string GEAR_KEY = "GearKey";

		private CallResult<LobbyEnter_t> m_OnLobbyEnteredCallresult;

		private CallResult<LobbyCreated_t> m_OnLobbyCreatedCallresult;

		private Callback<GameLobbyJoinRequested_t> m_OnLobbyJoinRequest;

		private Callback<LobbyInvite_t> m_OnInviteCallBack;

		private Callback<LobbyChatUpdate_t> m_OnLobbyUpdated;

		private Callback<LobbyDataUpdate_t> m_OnLobbyMemberDataUpdated;

		private Callback<LobbyChatMsg_t> m_OnLobbyChatMessage;

		private Action<string> m_OnLobbyCreatedActionString;

		private Action m_OnLobbyCreatedAction;

		private bool m_IsActive;

		private AppId_t ROUNDS_APPID = new AppId_t(1557740u);

		public bool IsMaster
		{
			get
			{
				return SteamMatchmaking.GetLobbyOwner(CurrentLobby) == SteamUser.GetSteamID();
			}
		}

		public bool IsInsideLobby
		{
			get
			{
				if (CurrentLobby.IsValid())
				{
					return CurrentLobby.IsLobby();
				}
				return false;
			}
		}

		public int NumberOfMembers
		{
			get
			{
				if (!m_IsActive)
				{
					return 0;
				}
				return SteamMatchmaking.GetNumLobbyMembers(CurrentLobby);
			}
		}

		public CSteamID CurrentLobby { get; private set; }

		public bool IsActive
		{
			get
			{
				return m_IsActive;
			}
			set
			{
				if (!SteamManager.Initialized)
				{
					m_IsActive = false;
				}
				else
				{
					m_IsActive = value;
				}
			}
		}

		public ClientSteamLobby()
		{
			m_IsActive = SteamManager.Initialized;
			if (m_IsActive)
			{
				CreateCallbacks();
			}
			CurrentLobby = CSteamID.Nil;
			CheckForCommandLine();
		}

		private void CheckForCommandLine()
		{
			string pszCommandLine;
			int launchCommandLine = SteamApps.GetLaunchCommandLine(out pszCommandLine, 260);
			if (pszCommandLine == null)
			{
				pszCommandLine = "";
			}
			Debug.LogError("Command Line: ret: " + launchCommandLine + " : " + pszCommandLine);
		}

		private void CreateCallbacks()
		{
			m_OnLobbyCreatedCallresult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
			m_OnLobbyEnteredCallresult = CallResult<LobbyEnter_t>.Create(OnLobbyEnter);
			m_OnLobbyJoinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequest);
			m_OnLobbyUpdated = Callback<LobbyChatUpdate_t>.Create(OnLobbyMembersUpdated);
			m_OnLobbyMemberDataUpdated = Callback<LobbyDataUpdate_t>.Create(OnLobbyMemberDataUpdated);
			m_OnLobbyChatMessage = Callback<LobbyChatMsg_t>.Create(OnLobbyChatMessage);
			m_OnInviteCallBack = Callback<LobbyInvite_t>.Create(OnInviteToLobby);
		}

		public void JoinedRoom(string roomName)
		{
			if (!m_IsActive)
			{
				return;
			}
			if (IsMaster)
			{
				byte[] roomData = GetRoomData(roomName);
				Debug.Log("Joined room: " + roomName + " And is master!");
				if (SendDataThroughLobby(roomData))
				{
					Debug.Log("Sent message to others in lobby about room");
				}
			}
			m_IsActive = false;
		}

		private byte[] GetRequestStartGameData()
		{
			if (!m_IsActive)
			{
				return new byte[0];
			}
			byte value = 3;
			byte[] array = new byte[2];
			using (MemoryStream output = new MemoryStream(array))
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(output))
				{
					binaryWriter.Write(value);
					return array;
				}
			}
		}

		private byte[] GetRoomData(string roomName)
		{
			if (!m_IsActive)
			{
				return new byte[0];
			}
			byte value = 1;
			byte[] array = new byte[2 * roomName.Length + 1];
			using (MemoryStream output = new MemoryStream(array))
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(output))
				{
					binaryWriter.Write(value);
					binaryWriter.Write(roomName);
					return array;
				}
			}
		}

		public void InviteFriend(CSteamID id)
		{
			bool flag = SteamMatchmaking.InviteUserToLobby(CurrentLobby, id);
			Debug.Log(string.Concat("Sent invite to friend: ", id, flag ? " SUCCESS!" : " FAIL"));
		}

		private bool SendDataThroughLobby(byte[] data)
		{
			return SteamMatchmaking.SendLobbyChatMsg(CurrentLobby, data, data.Length);
		}

		public void ShowInviteScreenWhenConnected()
		{
			AddOnLobbyCreatedAction(_003CShowInviteScreenWhenConnected_003Eb__35_0);
		}

		public void GetFriendsPlayingROUNDS(Action<List<CSteamID>> a)
		{
			List<CSteamID> list = new List<CSteamID>();
			for (int i = 0; i < NumberOfMembers; i++)
			{
				list.Add(SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobby, i));
			}
			List<CSteamID> list2 = new List<CSteamID>();
			int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
			for (int j = 0; j < friendCount; j++)
			{
				CSteamID friendByIndex = SteamFriends.GetFriendByIndex(j, EFriendFlags.k_EFriendFlagImmediate);
				FriendGameInfo_t pFriendGameInfo;
				if (SteamFriends.GetFriendGamePlayed(friendByIndex, out pFriendGameInfo) && pFriendGameInfo.m_gameID.AppID() == ROUNDS_APPID && !list.Contains(friendByIndex))
				{
					Debug.Log("Friend: " + j + " : " + SteamFriends.GetFriendPersonaName(friendByIndex) + " Is playing ROUNDS!");
					list2.Add(friendByIndex);
				}
			}
			a(list2);
		}

		public void CreateLobby(int maxPlayers, Action<string> OnSuccess)
		{
			if (m_IsActive)
			{
				m_OnLobbyCreatedActionString = OnSuccess;
				SteamAPICall_t hAPICall = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePrivate, maxPlayers);
				m_OnLobbyCreatedCallresult.Set(hAPICall);
			}
		}

		private void AddOnLobbyCreatedAction(Action a)
		{
			m_OnLobbyCreatedAction = (Action)Delegate.Combine(m_OnLobbyCreatedAction, a);
		}

		public void HideLobby()
		{
			if (m_IsActive && IsMaster)
			{
				SteamMatchmaking.SetLobbyJoinable(CurrentLobby, false);
			}
		}

		public void OpenLobby()
		{
			if (m_IsActive && IsMaster)
			{
				SteamMatchmaking.SetLobbyJoinable(CurrentLobby, true);
			}
		}

		private void JoinLobby(CSteamID lobbyID)
		{
			if (m_IsActive)
			{
				SteamAPICall_t hAPICall = SteamMatchmaking.JoinLobby(lobbyID);
				m_OnLobbyEnteredCallresult.Set(hAPICall);
			}
		}

		private bool CheckAppVersion(CSteamID lobbyID)
		{
			throw new NotImplementedException();
		}

		private void LeaveCurrentLobby()
		{
			if (m_IsActive && CurrentLobby.IsLobby())
			{
				SteamMatchmaking.LeaveLobby(CurrentLobby);
			}
		}

		public void GoOffline()
		{
			LeaveCurrentLobby();
			m_IsActive = false;
		}

		public void LeaveLobby()
		{
			LeaveCurrentLobby();
		}

		public string[] GetPhotonIDsOfMembers()
		{
			if (!m_IsActive)
			{
				return new string[0];
			}
			int numberOfMembers = NumberOfMembers;
			string[] result = new string[numberOfMembers];
			Debug.Log("Reserving spots for : " + numberOfMembers + " Players!");
			for (int i = 0; i < numberOfMembers; i++)
			{
				string friendPersonaName = SteamFriends.GetFriendPersonaName(SteamMatchmaking.GetLobbyMemberByIndex(CurrentLobby, i));
				Debug.Log("Member: " + i + " : " + friendPersonaName);
			}
			return result;
		}

		private void UpdateLobbyMembers()
		{
			if (m_IsActive)
			{
				Refresh();
			}
		}

		private void UpdateLobbyData()
		{
		}

		public void Refresh()
		{
		}

		private void UpdateCurrentLobby(CSteamID lobbyID)
		{
			CurrentLobby = lobbyID;
		}

		private void UpdateCurrentLobbyCallback()
		{
		}

		public void UpdateGear(string gearString)
		{
			if (m_IsActive)
			{
				SteamMatchmaking.SetLobbyMemberData(CurrentLobby, "GearKey", gearString);
			}
		}

		public void RequestStartGame()
		{
			throw new NotImplementedException();
		}

		public void ReadyUp(bool ready)
		{
			throw new NotImplementedException();
		}

		public void UpdateRegion()
		{
			throw new NotImplementedException();
		}

		public void UpdateMatchMode()
		{
			throw new NotImplementedException();
		}

		private void OnInviteToLobby(LobbyInvite_t param)
		{
			if (param.m_ulGameID == ROUNDS_APPID.m_AppId)
			{
				Debug.Log("Invite recieved from: " + param.m_ulSteamIDUser);
				SteamFriends.GetFriendPersonaName(new CSteamID(param.m_ulSteamIDUser));
			}
		}

		private void OnLobbyChatMessage(LobbyChatMsg_t param)
		{
			if (new CSteamID(param.m_ulSteamIDUser) == SteamUser.GetSteamID())
			{
				Debug.Log("Recieving callback for local message, returning...");
				return;
			}
			if (new CSteamID(param.m_ulSteamIDLobby) != CurrentLobby)
			{
				Debug.LogError("Recieved lobby message for another lobby, returning...");
				return;
			}
			EChatEntryType eChatEntryType = (EChatEntryType)param.m_eChatEntryType;
			if (eChatEntryType == EChatEntryType.k_EChatEntryTypeChatMsg)
			{
				int num = 60;
				byte[] pvData = new byte[num];
				CSteamID pSteamIDUser;
				EChatEntryType peChatEntryType;
				int lobbyChatEntry = SteamMatchmaking.GetLobbyChatEntry(CurrentLobby, (int)param.m_iChatID, out pSteamIDUser, pvData, num, out peChatEntryType);
				Debug.Log("Lobby Chsat Msg: " + lobbyChatEntry);
			}
			else
			{
				Debug.LogError("Recived unexpected lobbychat message: " + eChatEntryType);
			}
		}

		private void OnLobbyMembersUpdated(LobbyChatUpdate_t param)
		{
			uint rgfChatMemberStateChange = param.m_rgfChatMemberStateChange;
			if (new CSteamID(param.m_ulSteamIDLobby) == CurrentLobby)
			{
				UpdateLobbyMembers();
			}
			else
			{
				Debug.LogError("Getting update for another lobby!?");
			}
		}

		private void OnLobbyMemberDataUpdated(LobbyDataUpdate_t param)
		{
			if (new CSteamID(param.m_ulSteamIDLobby) == CurrentLobby)
			{
				UpdateLobbyMembers();
				UpdateLobbyData();
			}
			else
			{
				Debug.LogError("Getting Lobbychat update for another lobby!?");
			}
		}

		private void OnLobbyJoinRequest(GameLobbyJoinRequested_t param)
		{
			Debug.Log("Lobby Join Request! " + param.m_steamIDLobby);
			LeaveCurrentLobby();
			JoinLobby(param.m_steamIDLobby);
		}

		private void OnLobbyCreated(LobbyCreated_t param, bool bIOFailure)
		{
			InternalOnLobbyCreated(param, bIOFailure, m_OnLobbyCreatedActionString);
		}

		private void InternalOnLobbyCreated(LobbyCreated_t param, bool bIOFailure, Action<string> OnSuccess = null, Action OnFail = null)
		{
			if (bIOFailure)
			{
				Debug.LogError("BioFail");
			}
			else if (param.m_eResult == EResult.k_EResultOK)
			{
				Debug.Log("Successfully created A steam Lobby");
				UpdateCurrentLobby(new CSteamID(param.m_ulSteamIDLobby));
				SteamMatchmaking.SetLobbyData(CurrentLobby, "RegionKey", PhotonNetwork.CloudRegion);
				if (OnSuccess != null)
				{
					OnSuccess(param.m_ulSteamIDLobby.ToString());
				}
				Action onLobbyCreatedAction = m_OnLobbyCreatedAction;
				if (onLobbyCreatedAction != null)
				{
					onLobbyCreatedAction();
				}
				ClearActions();
			}
			else
			{
				Debug.Log("Failure creating A steam Lobby " + param.m_eResult);
			}
		}

		private void ClearActions()
		{
			m_OnLobbyCreatedAction = null;
			m_OnLobbyCreatedActionString = null;
		}

		private void OnLobbyEnter(LobbyEnter_t param, bool bIOFailure)
		{
			if (bIOFailure)
			{
				Debug.LogError("BioFail");
			}
			else if (param.m_EChatRoomEnterResponse == 1)
			{
				Debug.Log("Successfully Entered A steam Lobby");
				CSteamID cSteamID = new CSteamID(param.m_ulSteamIDLobby);
				UpdateCurrentLobby(cSteamID);
				if (SteamManager.Initialized)
				{
					string lobbyData = SteamMatchmaking.GetLobbyData(cSteamID, "RegionKey");
					NetworkConnectionHandler.instance.ForceRegionJoin(lobbyData, cSteamID.ToString());
				}
			}
		}

		[CompilerGenerated]
		private void _003CShowInviteScreenWhenConnected_003Eb__35_0()
		{
			Debug.Log("Activating SteamOverlay for INVITE: With Lobby: " + CurrentLobby);
			SteamFriends.ActivateGameOverlayInviteDialog(CurrentLobby);
		}
	}
}
