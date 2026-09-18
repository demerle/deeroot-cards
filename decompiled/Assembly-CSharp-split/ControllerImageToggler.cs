using System;
using UnityEngine;
using UnityEngine.UI;

public class ControllerImageToggler : MonoBehaviour
{
	public Sprite MKSprite;

	public Sprite controllerSprite;

	private Image img;

	private CharacterCreatorPortrait portrait;

	private CharacterSelectionInstance selector;

	private void Awake()
	{
		selector = GetComponentInParent<CharacterSelectionInstance>();
		portrait = GetComponentInParent<CharacterCreatorPortrait>();
		if ((bool)portrait && portrait.controlType != MenuControllerHandler.MenuControl.Unassigned)
		{
			Switch(portrait.controlType);
		}
	}

	private void Start()
	{
		img = GetComponent<Image>();
		MenuControllerHandler instance = MenuControllerHandler.instance;
		instance.switchControlAction = (Action<MenuControllerHandler.MenuControl>)Delegate.Combine(instance.switchControlAction, new Action<MenuControllerHandler.MenuControl>(Switch));
		if (!selector && portrait.controlType == MenuControllerHandler.MenuControl.Unassigned)
		{
			Switch(MenuControllerHandler.menuControl);
		}
	}

	private void Update()
	{
		if ((bool)selector && (bool)selector.currentPlayer)
		{
			if (selector.currentPlayer.data.input.inputType == GeneralInput.InputType.Controller)
			{
				img.sprite = controllerSprite;
			}
			else
			{
				img.sprite = MKSprite;
			}
		}
	}

	private void Switch(MenuControllerHandler.MenuControl control)
	{
		if (!img)
		{
			img = GetComponent<Image>();
		}
		if (!selector)
		{
			if (control == MenuControllerHandler.MenuControl.Controller)
			{
				img.sprite = controllerSprite;
			}
			else
			{
				img.sprite = MKSprite;
			}
		}
	}
}
