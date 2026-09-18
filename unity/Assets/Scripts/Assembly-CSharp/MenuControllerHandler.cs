using System;
using InControl;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuControllerHandler : MonoBehaviour
{
	public enum MenuControl
	{
		Controller = 0,
		Mouse = 1,
		Unassigned = 2
	}

	public static MenuControl menuControl;

	public MenuControl lastMenuControl;

	public Action<MenuControl> switchControlAction;

	public static MenuControllerHandler instance;

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		Switch();
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Mouse0))
		{
			menuControl = MenuControl.Mouse;
		}
		if (Input.GetKeyDown(KeyCode.Mouse1))
		{
			menuControl = MenuControl.Mouse;
		}
		for (int i = 0; i < InputManager.ActiveDevices.Count; i++)
		{
			InputDevice inputDevice = InputManager.ActiveDevices[i];
			if (!inputDevice.AnyButtonWasPressed && !(Mathf.Abs(inputDevice.LeftStick.Value.y) > 0.1f))
			{
				continue;
			}
			menuControl = MenuControl.Controller;
			if (EventSystem.current.currentSelectedGameObject.activeInHierarchy)
			{
				continue;
			}
			ListMenuButton[] array = UnityEngine.Object.FindObjectsOfType<ListMenuButton>();
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j].enabled)
				{
					ListMenu.instance.SelectButton(array[j]);
				}
			}
		}
		if (menuControl != lastMenuControl)
		{
			Switch();
		}
		lastMenuControl = menuControl;
	}

	private void Switch()
	{
		Action<MenuControl> action = switchControlAction;
		if (action != null)
		{
			action(menuControl);
		}
	}
}
