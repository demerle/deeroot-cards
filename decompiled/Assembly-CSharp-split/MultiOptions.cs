using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MultiOptions : MonoBehaviour
{
	private GameObject source;

	private OptionsButton targetButton;

	private void Start()
	{
	}

	private void Update()
	{
		if (Input.GetKeyUp(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Escape))
		{
			Close();
		}
	}

	internal void ClickRessButton(MultiOptionsButton buttonPressed)
	{
		targetButton.SetResolutionAndFullscreen(buttonPressed.currentRess, buttonPressed.currentFull);
		Close();
	}

	public void Open(OptionsButton.SettingsTarget settingsTarget, Vector3 pos, OptionsButton askingButton)
	{
		targetButton = askingButton;
		base.transform.position = pos;
		source = base.transform.GetChild(0).gameObject;
		if (settingsTarget == OptionsButton.SettingsTarget.Resolution)
		{
			PopulateRess(Screen.resolutions);
		}
		else
		{
			PopulateFullScreens(new Optionshandler.FullScreenOption[3]
			{
				Optionshandler.FullScreenOption.FullScreen,
				Optionshandler.FullScreenOption.WindowedFullScreen,
				Optionshandler.FullScreenOption.Windowed
			});
		}
		base.gameObject.SetActive(value: true);
		ListMenuButton[] componentsInChildren = base.transform.parent.GetComponentsInChildren<ListMenuButton>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].transform.parent != base.transform)
			{
				componentsInChildren[i].enabled = false;
			}
		}
		CanvasGroup[] componentsInChildren2 = base.transform.parent.GetComponentsInChildren<CanvasGroup>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			componentsInChildren2[j].alpha = 0.05f;
			componentsInChildren2[j].interactable = false;
		}
		if (MenuControllerHandler.menuControl == MenuControllerHandler.MenuControl.Controller)
		{
			StartCoroutine(WaitFrame());
		}
		else
		{
			ListMenu.instance.ClearBar();
		}
	}

	private IEnumerator WaitFrame()
	{
		yield return new WaitForSecondsRealtime(0f);
		ListMenu.instance.SelectButton(GetComponentInChildren<ListMenuButton>());
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
		ListMenuButton[] componentsInChildren = base.transform.parent.GetComponentsInChildren<ListMenuButton>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].transform.parent != base.transform)
			{
				componentsInChildren[i].enabled = true;
			}
		}
		CanvasGroup[] componentsInChildren2 = base.transform.parent.GetComponentsInChildren<CanvasGroup>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			componentsInChildren2[j].alpha = 1f;
			componentsInChildren2[j].interactable = true;
		}
		if (MenuControllerHandler.menuControl == MenuControllerHandler.MenuControl.Controller)
		{
			ListMenu.instance.SelectButton(targetButton.transform.GetComponent<ListMenuButton>());
		}
		else
		{
			ListMenu.instance.ClearBar();
		}
	}

	private void PopulateRess(Resolution[] allRess)
	{
		for (int num = base.transform.childCount - 1; num > 0; num--)
		{
			Object.Destroy(base.transform.GetChild(num).gameObject);
		}
		allRess = GetBestResolutions();
		for (int i = 0; i < allRess.Length; i++)
		{
			GameObject obj = Object.Instantiate(source, source.transform.position, source.transform.rotation, source.transform.parent);
			obj.GetComponentInChildren<TextMeshProUGUI>().text = allRess[i].width + " x " + allRess[i].height;
			obj.GetComponent<MultiOptionsButton>().currentRess = allRess[i];
			obj.gameObject.SetActive(value: true);
		}
	}

	private Resolution[] GetBestResolutions()
	{
		List<Resolution> list = new List<Resolution>();
		for (int i = 0; i < Screen.resolutions.Length; i++)
		{
			if (IsBestRess(Screen.resolutions[i]))
			{
				list.Add(Screen.resolutions[i]);
			}
		}
		return list.ToArray();
	}

	private bool IsBestRess(Resolution newRess)
	{
		for (int i = 0; i < Screen.resolutions.Length; i++)
		{
			if (Screen.resolutions[i].width == newRess.width && Screen.resolutions[i].height == newRess.height && newRess.refreshRate < Screen.resolutions[i].refreshRate)
			{
				return false;
			}
		}
		return true;
	}

	private void PopulateFullScreens(Optionshandler.FullScreenOption[] allScreens)
	{
		for (int num = base.transform.childCount - 1; num > 0; num--)
		{
			Object.Destroy(base.transform.GetChild(num).gameObject);
		}
		for (int i = 0; i < allScreens.Length; i++)
		{
			GameObject gameObject = Object.Instantiate(source, source.transform.position, source.transform.rotation, source.transform.parent);
			if (allScreens[i] == Optionshandler.FullScreenOption.WindowedFullScreen)
			{
				gameObject.GetComponentInChildren<TextMeshProUGUI>().text = "WINDOWED FULLSCREEN";
			}
			else
			{
				gameObject.GetComponentInChildren<TextMeshProUGUI>().text = allScreens[i].ToString().ToUpper();
			}
			gameObject.GetComponent<MultiOptionsButton>().currentFull = allScreens[i];
			gameObject.gameObject.SetActive(value: true);
		}
	}
}
