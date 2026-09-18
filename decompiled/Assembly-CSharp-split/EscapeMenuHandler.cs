using InControl;
using UnityEngine;

public class EscapeMenuHandler : MonoBehaviour
{
	public static bool isEscMenu;

	public GameObject[] togglers;

	public GameObject canvs;

	private void Start()
	{
		isEscMenu = false;
	}

	private void Update()
	{
		if (CharacterCreatorHandler.instance.SomeoneIsEditing() || canvs.activeInHierarchy)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			ToggleEsc();
		}
		for (int i = 0; i < InputManager.ActiveDevices.Count; i++)
		{
			if (InputManager.ActiveDevices[i].CommandWasPressed)
			{
				ToggleEsc();
			}
		}
	}

	public void ToggleEsc()
	{
		isEscMenu = !isEscMenu;
		if (isEscMenu)
		{
			ListMenu.instance.SelectButton(GetComponentInChildren<ListMenuPage>().firstSelected);
		}
		else
		{
			ListMenu.instance.SelectButton(GetComponentsInChildren<ListMenuButton>()[1]);
		}
		for (int i = 0; i < togglers.Length; i++)
		{
			togglers[i].SetActive(isEscMenu);
		}
		if (isEscMenu)
		{
			ListMenu.instance.SelectButton(GetComponentInChildren<ListMenuPage>().firstSelected);
			return;
		}
		ListMenuButton[] componentsInChildren = GetComponentsInChildren<ListMenuButton>(includeInactive: true);
		if (componentsInChildren.Length > 2)
		{
			ListMenu.instance.SelectButton(componentsInChildren[1]);
		}
	}
}
