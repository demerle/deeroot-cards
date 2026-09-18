using TMPro;
using UnityEngine;

public class ErrorHandler : MonoBehaviour
{
	public static ErrorHandler instance;

	public TextMeshProUGUI contextText;

	public TextMeshProUGUI reasonText;

	public GameObject UI;

	private void Awake()
	{
		instance = this;
	}

	public void ShowError(string context, string reason)
	{
		contextText.text = context;
		reasonText.text = reason;
		UI.SetActive(value: true);
	}

	public void HideError()
	{
		UI.SetActive(value: false);
	}
}
