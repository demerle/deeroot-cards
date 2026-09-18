using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEngine;

public class Player : MonoBehaviour
{
	public int playerID;

	public int teamID;

	[HideInInspector]
	public CharacterData data;

	public PlayerSkinBank colors;

	private void Awake()
	{
		data = GetComponent<CharacterData>();
	}

	private void Start()
	{
		if (!data.view.IsMine)
		{
			ReadPlayerID();
			ReadTeamID();
			PlayerAssigner.instance.OtherPlayerWasCreated();
		}
		else
		{
			int num = 0;
			if (!PhotonNetwork.OfflineMode)
			{
				try
				{
					num = 0;
					PlayerFace playerFace = CharacterCreatorHandler.instance.selectedPlayerFaces[num];
					data.view.RPC("RPCA_SetFace", RpcTarget.All, playerFace.eyeID, playerFace.eyeOffset, playerFace.mouthID, playerFace.mouthOffset, playerFace.detailID, playerFace.detailOffset, playerFace.detail2ID, playerFace.detail2Offset);
				}
				catch
				{
				}
			}
			else if (GM_ArmsRace.instance != null)
			{
				GM_ArmsRace instance = GM_ArmsRace.instance;
				instance.StartGameAction = (Action)Delegate.Combine(instance.StartGameAction, new Action(GetFaceOffline));
			}
		}
		PlayerManager.instance.PlayerJoined(this);
	}

	public void GetFaceOffline()
	{
		PlayerFace playerFace = CharacterCreatorHandler.instance.selectedPlayerFaces[playerID];
		data.view.RPC("RPCA_SetFace", RpcTarget.All, playerFace.eyeID, playerFace.eyeOffset, playerFace.mouthID, playerFace.mouthOffset, playerFace.detailID, playerFace.detailOffset, playerFace.detail2ID, playerFace.detail2Offset);
	}

	[PunRPC]
	public void RPCA_SetFace(int eyeID, Vector2 eyeOffset, int mouthID, Vector2 mouthOffset, int detailID, Vector2 detailOffset, int detail2ID, Vector2 detail2Offset)
	{
		PlayerFace face = PlayerFace.CreateFace(eyeID, eyeOffset, mouthID, mouthOffset, detailID, detailOffset, detail2ID, detail2Offset);
		GetComponentInChildren<CharacterCreatorItemEquipper>().EquipFace(face);
	}

	internal void Call_AllGameFeel(Vector2 vector2)
	{
		data.view.RPC("RPCA_AllGameFeel", RpcTarget.All, vector2);
	}

	[PunRPC]
	internal void RPCA_AllGameFeel(Vector2 vector2)
	{
		GamefeelManager.instance.AddGameFeel(vector2);
	}

	public void AssignPlayerID(int ID)
	{
		playerID = ID;
		SetColors();
		if (!PhotonNetwork.OfflineMode)
		{
			Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
			if (customProperties.ContainsKey("PlayerID"))
			{
				customProperties["PlayerID"] = playerID;
			}
			else
			{
				customProperties.Add("PlayerID", playerID);
			}
			PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);
		}
	}

	internal float GetRadius()
	{
		return 5f;
	}

	private void ReadPlayerID()
	{
		if (!PhotonNetwork.OfflineMode)
		{
			playerID = (int)data.view.Owner.CustomProperties["PlayerID"];
			SetColors();
		}
	}

	public void AssignTeamID(int ID)
	{
		teamID = ID;
		if (!PhotonNetwork.OfflineMode)
		{
			Hashtable customProperties = PhotonNetwork.LocalPlayer.CustomProperties;
			if (customProperties.ContainsKey("TeamID"))
			{
				customProperties["TeamID"] = playerID;
			}
			else
			{
				customProperties.Add("TeamID", teamID);
			}
			PhotonNetwork.LocalPlayer.SetCustomProperties(customProperties);
		}
	}

	private void ReadTeamID()
	{
		if (!PhotonNetwork.OfflineMode)
		{
			teamID = (int)data.view.Owner.CustomProperties["TeamID"];
		}
	}

	public void SetColors()
	{
		SetTeamColor.TeamColorThis(base.gameObject, PlayerSkinBank.GetPlayerSkinColors(playerID));
	}

	public PlayerSkin GetTeamColors()
	{
		return PlayerSkinBank.GetPlayerSkinColors(playerID);
	}

	internal void FullReset()
	{
		data.weaponHandler.NewGun();
		data.stats.ResetStats();
		data.block.ResetStats();
	}
}
