using System;
using System.Collections.Generic;
using UnityEngine;

public class CharacterCreatorHandler : MonoBehaviour
{
	public static CharacterCreatorHandler instance;

	public PlayerFace[] playerFaces = new PlayerFace[10];

	public PlayerFace[] selectedPlayerFaces = new PlayerFace[4];

	public int[] selectedFaceID = new int[4];

	public List<int> lockedPortraits;

	public Action lockedPortraitAction;

	public Action<int> faceWasUpdatedAction;

	public void LockPortrait(int portrait)
	{
		lockedPortraits.Add(portrait);
		lockedPortraitAction?.Invoke();
	}

	public void ReleasePortrait(int porttrait)
	{
		for (int i = 0; i < lockedPortraits.Count; i++)
		{
			if (porttrait == lockedPortraits[i])
			{
				lockedPortraits.RemoveAt(i);
				break;
			}
		}
		lockedPortraitAction?.Invoke();
	}

	private void ReleaseAllPortraits()
	{
		lockedPortraits.Clear();
		lockedPortraitAction?.Invoke();
	}

	private void Awake()
	{
		instance = this;
		for (int i = 0; i < playerFaces.Length; i++)
		{
			playerFaces[i].LoadFace(i.ToString());
		}
		for (int j = 0; j < selectedFaceID.Length; j++)
		{
			selectedFaceID[j] = PlayerPrefs.GetInt("SelectedFace" + j);
			SelectFace(j, playerFaces[selectedFaceID[j]], selectedFaceID[j]);
		}
	}

	internal void SelectFace(int faceID, PlayerFace selectedFace, int faceSlot)
	{
		selectedFaceID[faceID] = faceSlot;
		PlayerPrefs.SetInt("SelectedFace" + faceID, selectedFaceID[faceID]);
		selectedPlayerFaces[faceID] = selectedFace;
	}

	public PlayerFace GetFacePreset(int faceID)
	{
		return playerFaces[faceID];
	}

	public bool SomeoneIsEditing()
	{
		for (int i = 0; i < base.transform.childCount; i++)
		{
			if (base.transform.GetChild(i).gameObject.activeSelf)
			{
				return true;
			}
		}
		return false;
	}

	internal void SetFacePreset(int faceID, PlayerFace currentPlayerFace)
	{
		playerFaces[faceID] = PlayerFace.CopyFace(currentPlayerFace);
		playerFaces[faceID].SaveFace(faceID.ToString());
		faceWasUpdatedAction?.Invoke(faceID);
	}

	public void EditCharacterLocalMultiplayer(int playerId, int portraitID, GameObject objectToEnable, PlayerFace currentFace)
	{
		LockPortrait(portraitID);
		CharacterCreator component = base.transform.GetChild(playerId + 1).GetComponent<CharacterCreator>();
		component.playerActions = PlayerManager.instance.players[playerId].data.playerActions;
		component.inputType = PlayerManager.instance.players[playerId].data.input.inputType;
		component.gameObject.SetActive(value: true);
		component.playerID = playerId;
		component.objectToEnable = objectToEnable;
		component.currentPlayerFace = currentFace;
		component.portraitID = portraitID;
		component.SpawnFace(currentFace);
	}

	public void EditCharacterPortrait(int portraitID, PlayerFace currentFace)
	{
		LockPortrait(portraitID);
		MainMenuHandler.instance.Close();
		CharacterCreator component = base.transform.GetChild(0).GetComponent<CharacterCreator>();
		component.inputType = GeneralInput.InputType.Either;
		component.currentPlayerFace = currentFace;
		component.gameObject.SetActive(value: true);
		component.playerID = 0;
		component.portraitID = portraitID;
		component.SpawnFace(currentFace);
	}

	public void CloseMenus()
	{
		for (int i = 0; i < base.transform.childCount; i++)
		{
			base.transform.GetChild(i).GetComponent<CharacterCreator>().Close();
		}
		ReleaseAllPortraits();
	}

	public void EndCustomization(int playerId = -1)
	{
		if (playerId == -1)
		{
			GetComponentInChildren<CharacterCreator>(includeInactive: true).Finish();
			base.transform.GetChild(0).gameObject.SetActive(value: false);
		}
	}
}
