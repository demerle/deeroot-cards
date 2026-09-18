using System;
using UnityEngine;

public class CharacterCreator : MonoBehaviour
{
	public PlayerFace currentPlayerFace;

	public GameObject objectToEnable;

	public int playerID;

	public int portraitID;

	public bool ready;

	public PlayerActions playerActions;

	public GeneralInput.InputType inputType;

	public MenuControllerHandler.MenuControl currentControl = MenuControllerHandler.MenuControl.Unassigned;

	public MenuControllerHandler.MenuControl lastControl = MenuControllerHandler.MenuControl.Unassigned;

	public Action<MenuControllerHandler.MenuControl> SwitchAction;

	public CharacterCreatorNavigation nav;

	private void Start()
	{
		nav = GetComponentInChildren<CharacterCreatorNavigation>();
	}

	private void Update()
	{
		if (currentControl != lastControl)
		{
			SwitchAction?.Invoke(currentControl);
		}
		lastControl = currentControl;
	}

	public void Close()
	{
		CharacterCreatorHandler.instance.ReleasePortrait(portraitID);
		if (playerActions == null)
		{
			base.gameObject.SetActive(value: false);
			MainMenuHandler.instance.Open();
		}
		else
		{
			objectToEnable.SetActive(value: true);
			base.gameObject.SetActive(value: false);
		}
	}

	public void Finish()
	{
		CharacterCreatorHandler.instance.ReleasePortrait(portraitID);
		CharacterCreatorHandler.instance.SetFacePreset(portraitID, currentPlayerFace);
		CharacterCreatorHandler.instance.SelectFace(0, currentPlayerFace, portraitID);
		if (playerActions == null)
		{
			base.gameObject.SetActive(value: false);
			MainMenuHandler.instance.Open();
		}
		else
		{
			objectToEnable.SetActive(value: true);
			base.gameObject.SetActive(value: false);
		}
	}

	internal void SetOffset(Vector2 offset, CharacterItemType itemType, int slotID)
	{
		if (itemType == CharacterItemType.Eyes)
		{
			currentPlayerFace.eyeOffset = offset;
		}
		if (itemType == CharacterItemType.Mouth)
		{
			currentPlayerFace.mouthOffset = offset;
		}
		if (itemType == CharacterItemType.Detail)
		{
			if (slotID == 0)
			{
				currentPlayerFace.detailOffset = offset;
			}
			if (slotID == 1)
			{
				currentPlayerFace.detail2Offset = offset;
			}
		}
	}

	internal void SpawnFace(PlayerFace currentFace)
	{
		GetComponentInChildren<CharacterCreatorItemEquipper>().SpawnPlayerFace(currentFace);
	}
}
