using System.Collections;
using SoundImplementation;
using UnityEngine;
using UnityEngine.EventSystems;

public class ListMenu : MonoBehaviour
{
	[Header("Settings")]
	public GameObject bar;

	[Header("Settings")]
	public GameObject particle;

	public ListMenuButton selectedButton;

	public ListMenuPage selectedPage;

	public static ListMenu instance;

	public GameObject menuCanvas;

	private bool isActive = true;

	private Vector3 lastPos;

	private bool playButtonHoverFirst = true;

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		GetComponentInChildren<ListMenuPage>(includeInactive: true).Open();
	}

	private void Update()
	{
		if ((lastPos - bar.transform.position).sqrMagnitude > 0.5f && (MainMenuHandler.instance.isOpen || EscapeMenuHandler.isEscMenu))
		{
			if (!playButtonHoverFirst)
			{
				SoundPlayerStatic.Instance.PlayButtonHover();
			}
			else
			{
				playButtonHoverFirst = false;
			}
		}
		lastPos = bar.transform.position;
		if ((bool)selectedButton && EventSystem.current.currentSelectedGameObject == null)
		{
			EventSystem.current.SetSelectedGameObject(selectedButton.gameObject);
		}
		if (EscapeMenuHandler.isEscMenu || menuCanvas.activeInHierarchy)
		{
			if (!bar.activeSelf)
			{
				bar.SetActive(value: true);
				particle.SetActive(value: true);
			}
		}
		else if (bar.activeSelf)
		{
			bar.SetActive(value: false);
			particle.SetActive(value: false);
		}
	}

	internal void DeselectButton()
	{
		if ((bool)selectedButton)
		{
			selectedButton.Deselect();
		}
	}

	public void OpenPage(ListMenuPage pageToOpen)
	{
		if ((bool)selectedPage)
		{
			selectedPage.Close();
		}
		bar.transform.localScale = new Vector3(bar.transform.localScale.x, pageToOpen.barHeight, 1f);
		selectedPage = pageToOpen;
		if (MenuControllerHandler.menuControl == MenuControllerHandler.MenuControl.Controller)
		{
			SelectButton(selectedPage.firstSelected);
		}
		else
		{
			bar.transform.position = Vector3.up * 10000f;
		}
	}

	public void SelectButton(ListMenuButton buttonToSelect)
	{
		if (!buttonToSelect || buttonToSelect.hideBar)
		{
			bar.transform.position = Vector3.up * 10000f;
		}
		else
		{
			StartCoroutine(ISelectButton(buttonToSelect));
		}
	}

	public IEnumerator ISelectButton(ListMenuButton buttonToSelect)
	{
		if ((bool)selectedButton)
		{
			selectedButton.Deselect();
		}
		yield return new WaitForEndOfFrame();
		buttonToSelect.Select();
		selectedButton = buttonToSelect;
		bar.transform.position = buttonToSelect.transform.position;
		if (EventSystem.current.currentSelectedGameObject != selectedButton.gameObject)
		{
			EventSystem.current.SetSelectedGameObject(selectedButton.gameObject);
		}
		if (buttonToSelect.setBarHeight != 0f)
		{
			bar.transform.localScale = new Vector3(bar.transform.localScale.x, buttonToSelect.setBarHeight, 1f);
		}
	}

	public void ClearBar()
	{
		if (MenuControllerHandler.menuControl != MenuControllerHandler.MenuControl.Controller)
		{
			bar.transform.position = Vector3.up * 10000f;
		}
	}
}
