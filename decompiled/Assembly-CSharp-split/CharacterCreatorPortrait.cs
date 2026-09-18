using System;
using InControl;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCreatorPortrait : MonoBehaviour
{
	public int playerId = -1;

	public PlayerFace myFace;

	public GameObject selectedObj;

	public MenuControllerHandler.MenuControl controlType = MenuControllerHandler.MenuControl.Unassigned;

	private Color defColor;

	private HoverEvent hoverEvent;

	public GameObject lockedObj;

	private bool isLocked;

	private void Start()
	{
		defColor = base.transform.Find("BG").GetComponent<Image>().color;
		CharacterCreatorHandler instance = CharacterCreatorHandler.instance;
		instance.faceWasUpdatedAction = (Action<int>)Delegate.Combine(instance.faceWasUpdatedAction, new Action<int>(FaceUpdated));
		myFace = CharacterCreatorHandler.instance.GetFacePreset(base.transform.GetSiblingIndex());
		hoverEvent = GetComponent<HoverEvent>();
		GetComponentInChildren<CharacterCreatorItemEquipper>().EquipFace(myFace);
		if (CharacterCreatorHandler.instance.selectedFaceID[0] == base.transform.GetSiblingIndex())
		{
			ClickButton();
		}
		CharacterCreatorHandler instance2 = CharacterCreatorHandler.instance;
		instance2.lockedPortraitAction = (Action)Delegate.Combine(instance2.lockedPortraitAction, new Action(CheckLocked));
		CheckLocked();
	}

	private void CheckLocked()
	{
		isLocked = false;
		for (int i = 0; i < CharacterCreatorHandler.instance.lockedPortraits.Count; i++)
		{
			if (base.transform.GetSiblingIndex() == CharacterCreatorHandler.instance.lockedPortraits[i])
			{
				isLocked = true;
			}
		}
		if (isLocked)
		{
			lockedObj.SetActive(value: true);
		}
		else
		{
			lockedObj.SetActive(value: false);
		}
	}

	private void FaceUpdated(int faceID)
	{
		if (faceID == base.transform.GetSiblingIndex())
		{
			myFace = CharacterCreatorHandler.instance.GetFacePreset(faceID);
			GetComponent<CharacterCreatorItemEquipper>().EquipFace(myFace);
		}
	}

	public void ClickButton()
	{
		CharacterCreatorHandler.instance.SelectFace(Mathf.Clamp(playerId, 0, 10), myFace, base.transform.GetSiblingIndex());
		ShownFace();
	}

	private void ShownFace()
	{
		for (int i = 0; i < base.transform.parent.childCount; i++)
		{
			if (base.transform.parent.GetChild(i) == base.transform)
			{
				base.transform.parent.GetChild(i).Find("Frame").gameObject.SetActive(value: true);
			}
			else
			{
				base.transform.parent.GetChild(i).Find("Frame").gameObject.SetActive(value: false);
			}
		}
	}

	public void EditCharacter()
	{
		if (!isLocked)
		{
			ClickButton();
			if (playerId == -1)
			{
				CharacterCreatorHandler.instance.EditCharacterPortrait(base.transform.GetSiblingIndex(), myFace);
				return;
			}
			GameObject gameObject = base.transform.parent.parent.parent.gameObject;
			gameObject.SetActive(value: false);
			CharacterCreatorHandler.instance.EditCharacterLocalMultiplayer(playerId, base.transform.GetSiblingIndex(), gameObject, myFace);
		}
	}

	private void Update()
	{
		for (int i = 0; i < InputManager.ActiveDevices.Count; i++)
		{
			if (InputManager.ActiveDevices[i].Action4.WasPressed && hoverEvent.isSelected)
			{
				EditCharacter();
			}
		}
		if (Input.GetKeyDown(KeyCode.Mouse1) && controlType != MenuControllerHandler.MenuControl.Controller && hoverEvent.isHovered)
		{
			EditCharacter();
		}
	}
}
