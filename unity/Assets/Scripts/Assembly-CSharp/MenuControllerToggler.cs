using System;
using UnityEngine;

public class MenuControllerToggler : MonoBehaviour
{
	public bool creatorControl;

	public GameObject controllerObject;

	public GameObject keyboardObject;

	private CharacterCreator creator;

	private void Awake()
	{
		CharacterCreator componentInParent = GetComponentInParent<CharacterCreator>();
		if (componentInParent.playerActions == null)
		{
			if (creatorControl)
			{
				componentInParent.SwitchAction = (Action<MenuControllerHandler.MenuControl>)Delegate.Combine(componentInParent.SwitchAction, new Action<MenuControllerHandler.MenuControl>(Switch));
				Switch(GetComponentInParent<CharacterCreator>().currentControl);
			}
			else
			{
				MenuControllerHandler instance = MenuControllerHandler.instance;
				instance.switchControlAction = (Action<MenuControllerHandler.MenuControl>)Delegate.Combine(instance.switchControlAction, new Action<MenuControllerHandler.MenuControl>(Switch));
				Switch(MenuControllerHandler.menuControl);
			}
		}
	}

	private void OnEnable()
	{
		CharacterCreator componentInParent = GetComponentInParent<CharacterCreator>();
		if (componentInParent.playerActions != null)
		{
			componentInParent.SwitchAction = (Action<MenuControllerHandler.MenuControl>)Delegate.Combine(componentInParent.SwitchAction, new Action<MenuControllerHandler.MenuControl>(Switch));
			if (componentInParent.inputType == GeneralInput.InputType.Controller)
			{
				Switch(MenuControllerHandler.MenuControl.Controller);
			}
			if (componentInParent.inputType == GeneralInput.InputType.Keyboard)
			{
				Switch(MenuControllerHandler.MenuControl.Mouse);
			}
		}
	}

	private void Switch(MenuControllerHandler.MenuControl control)
	{
		if (control == MenuControllerHandler.MenuControl.Controller)
		{
			if ((bool)controllerObject)
			{
				controllerObject.SetActive(true);
			}
			if ((bool)keyboardObject)
			{
				keyboardObject.SetActive(false);
			}
		}
		else
		{
			if ((bool)controllerObject)
			{
				controllerObject.SetActive(false);
			}
			if ((bool)keyboardObject)
			{
				keyboardObject.SetActive(true);
			}
		}
	}
}
