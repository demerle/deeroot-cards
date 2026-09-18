using Photon.Pun;
using TMPro;
using UnityEngine;

public class OnlineNameSelect : MonoBehaviour
{
	private TMP_InputField nameField;

	private void Start()
	{
		nameField = GetComponentInChildren<TMP_InputField>();
		nameField.text = PlayerPrefs.GetString("PlayerName", "");
		PhotonNetwork.LocalPlayer.NickName = nameField.text;
	}

	public void OnChangedVal(string newVal)
	{
		PlayerPrefs.SetString("PlayerName", nameField.text);
		PhotonNetwork.LocalPlayer.NickName = nameField.text;
	}
}
