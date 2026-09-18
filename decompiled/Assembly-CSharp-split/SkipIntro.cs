using InControl;
using UnityEngine;

public class SkipIntro : MonoBehaviour
{
	public static bool hasShown;

	public ListMenuPage target;

	private void Start()
	{
		if (hasShown)
		{
			Skip();
		}
		hasShown = true;
	}

	private void Update()
	{
		for (int i = 0; i < InputManager.ActiveDevices.Count; i++)
		{
			if (InputManager.ActiveDevices[i].AnyButton.WasPressed)
			{
				Skip();
			}
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Skip();
		}
	}

	private void Skip()
	{
		target.Open();
	}
}
