using System.Collections;
using System.Collections.Generic;
using InControl;
using Photon.Pun;
using SoundImplementation;
using UnityEngine;

public class PlayerAssigner : MonoBehaviour
{
	public GameObject player1AI;

	public GameObject player2AI;

	public static PlayerAssigner instance;

	public GameObject playerPrefab;

	public int maxPlayers = 4;

	public List<CharacterData> players = new List<CharacterData>(4);

	private bool playersCanJoin;

	private int playerIDToSet = -1;

	private int teamIDToSet = -1;

	private bool waitingForRegisterResponse;

	private bool hasCreatedLocalPlayer;

	private void Awake()
	{
		instance = this;
	}

	internal void SetPlayersCanJoin(bool canJoin)
	{
		playersCanJoin = canJoin;
	}

	private void Start()
	{
		InputManager.OnDeviceDetached += OnDeviceDetached;
	}

	private void LateUpdate()
	{
		if (!playersCanJoin || players.Count >= maxPlayers || DevConsole.isTyping)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.B) && !GameManager.lockInput)
		{
			StartCoroutine(CreatePlayer(null, true));
		}
		if (Input.GetKey(KeyCode.Space))
		{
			bool flag = true;
			for (int i = 0; i < players.Count; i++)
			{
				if (players[i].playerActions.Device == null)
				{
					flag = false;
				}
			}
			if (flag)
			{
				StartCoroutine(CreatePlayer(null));
			}
		}
		for (int j = 0; j < InputManager.ActiveDevices.Count; j++)
		{
			InputDevice inputDevice = InputManager.ActiveDevices[j];
			if (JoinButtonWasPressedOnDevice(inputDevice) && ThereIsNoPlayerUsingDevice(inputDevice))
			{
				StartCoroutine(CreatePlayer(inputDevice));
			}
		}
	}

	private bool JoinButtonWasPressedOnDevice(InputDevice inputDevice)
	{
		if (!inputDevice.Action1.WasPressed && !inputDevice.Action2.WasPressed && !inputDevice.Action3.WasPressed)
		{
			return inputDevice.Action4.WasPressed;
		}
		return true;
	}

	private CharacterData FindPlayerUsingDevice(InputDevice inputDevice)
	{
		int count = players.Count;
		for (int i = 0; i < count; i++)
		{
			CharacterData characterData = players[i];
			if (characterData.playerActions.Device == inputDevice)
			{
				return characterData;
			}
		}
		return null;
	}

	public void ClearPlayers()
	{
		players.Clear();
	}

	private bool ThereIsNoPlayerUsingDevice(InputDevice inputDevice)
	{
		return FindPlayerUsingDevice(inputDevice) == null;
	}

	private void OnDeviceDetached(InputDevice inputDevice)
	{
		CharacterData characterData = FindPlayerUsingDevice(inputDevice);
		if (characterData != null)
		{
			RemovePlayer(characterData);
		}
	}

	[PunRPC]
	public void RPCM_RequestTeamAndPlayerID(int askingPlayer)
	{
		int count = PlayerManager.instance.players.Count;
		int num = ((count % 2 != 0) ? 1 : 0);
		GetComponent<PhotonView>().RPC("RPC_ReturnPlayerAndTeamID", PhotonNetwork.CurrentRoom.GetPlayer(askingPlayer), count, num);
		waitingForRegisterResponse = true;
	}

	[PunRPC]
	public void RPC_ReturnPlayerAndTeamID(int teamId, int playerID)
	{
		waitingForRegisterResponse = false;
		playerIDToSet = playerID;
		teamIDToSet = teamId;
	}

	public void OtherPlayerWasCreated()
	{
		waitingForRegisterResponse = false;
	}

	public IEnumerator CreatePlayer(InputDevice inputDevice, bool isAI = false)
	{
		if (waitingForRegisterResponse || (!PhotonNetwork.OfflineMode && hasCreatedLocalPlayer) || players.Count >= maxPlayers)
		{
			yield break;
		}
		if (!PhotonNetwork.OfflineMode && !PhotonNetwork.IsMasterClient)
		{
			GetComponent<PhotonView>().RPC("RPCM_RequestTeamAndPlayerID", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
			waitingForRegisterResponse = true;
		}
		while (waitingForRegisterResponse)
		{
			yield return null;
		}
		if (!PhotonNetwork.OfflineMode)
		{
			if (PhotonNetwork.IsMasterClient)
			{
				playerIDToSet = PlayerManager.instance.players.Count;
				teamIDToSet = ((playerIDToSet % 2 != 0) ? 1 : 0);
			}
		}
		else
		{
			playerIDToSet = PlayerManager.instance.players.Count;
			teamIDToSet = ((playerIDToSet % 2 != 0) ? 1 : 0);
		}
		hasCreatedLocalPlayer = true;
		SoundPlayerStatic.Instance.PlayPlayerAdded();
		Vector3 position = Vector3.up * 100f;
		CharacterData component = PhotonNetwork.Instantiate(playerPrefab.name, position, Quaternion.identity, 0).GetComponent<CharacterData>();
		if (isAI)
		{
			GameObject original = player1AI;
			if (players.Count > 0)
			{
				original = player2AI;
			}
			component.GetComponent<CharacterData>().SetAI();
			Object.Instantiate(original, component.transform.position, component.transform.rotation, component.transform);
		}
		else
		{
			if (inputDevice != null)
			{
				component.input.inputType = GeneralInput.InputType.Controller;
				component.playerActions = PlayerActions.CreateWithControllerBindings();
			}
			else
			{
				component.input.inputType = GeneralInput.InputType.Keyboard;
				component.playerActions = PlayerActions.CreateWithKeyboardBindings();
			}
			component.playerActions.Device = inputDevice;
		}
		players.Add(component);
		RegisterPlayer(component, teamIDToSet, playerIDToSet);
	}

	private void RegisterPlayer(CharacterData player, int teamID, int playerID)
	{
		PlayerManager.RegisterPlayer(player.player);
		player.player.AssignPlayerID(playerID);
		player.player.AssignTeamID(teamID);
	}

	private void RemovePlayer(CharacterData player)
	{
	}

	private void AssignPlayer(CharacterData player, InputDevice device)
	{
	}
}
