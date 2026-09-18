using UnityEngine;

public class MainMenuHandler : MonoBehaviour
{
	public static MainMenuHandler instance;

	public bool isOpen = true;

	private void Awake()
	{
		instance = this;
	}

	public void Close()
	{
		isOpen = false;
		base.transform.GetChild(0).gameObject.SetActive(false);
	}

	public void Open()
	{
		isOpen = true;
		base.transform.GetChild(0).gameObject.SetActive(true);
	}
}
