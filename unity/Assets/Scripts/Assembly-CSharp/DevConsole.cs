using Photon.Pun;
using TMPro;
using UnityEngine;

public class DevConsole : MonoBehaviour
{
	public TMP_InputField inputField;

	public static bool isTyping;

	private void Start()
	{
		isTyping = false;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return))
		{
			inputField.gameObject.SetActive(!inputField.gameObject.activeSelf);
			isTyping = inputField.gameObject.activeSelf;
			GameManager.lockInput = isTyping;
			if (inputField.gameObject.activeSelf)
			{
				inputField.ActivateInputField();
			}
			else
			{
				Send(inputField.text);
			}
		}
	}

	private void Send(string message)
	{
		if (Application.isEditor || ((bool)GM_Test.instance && GM_Test.instance.gameObject.activeSelf))
		{
			SpawnCard(message);
		}
		if (Application.isEditor)
		{
			SpawnMap(message);
		}
		int viewID = PlayerManager.instance.GetPlayerWithActorID(PhotonNetwork.LocalPlayer.ActorNumber).data.view.ViewID;
		GetComponent<PhotonView>().RPC("RPCA_SendChat", RpcTarget.All, message, viewID);
	}

	private void SpawnMap(string message)
	{
		try
		{
			int iD = int.Parse(message);
			MapManager.instance.LoadLevelFromID(iD, false, true);
		}
		catch
		{
		}
	}

	[PunRPC]
	private void RPCA_SendChat(string message, int playerViewID)
	{
		PhotonNetwork.GetPhotonView(playerViewID).GetComponentInChildren<PlayerChat>().Send(message);
	}

	private void SpawnCard(string message)
	{
		CardInfo[] cards = CardChoice.instance.cards;
		int num = -1;
		float num2 = 0f;
		for (int i = 0; i < cards.Length; i++)
		{
			string text = cards[i].GetComponent<CardInfo>().cardName.ToUpper();
			text = text.Replace(" ", "");
			string text2 = message.ToUpper();
			text2 = text2.Replace(" ", "");
			float num3 = 0f;
			for (int j = 0; j < text2.Length; j++)
			{
				if (text.Length > j && text2[j] == text[j])
				{
					num3 += 1f / (float)text2.Length;
				}
			}
			num3 -= (float)Mathf.Abs(text2.Length - text.Length) * 0.001f;
			if (num3 > 0.1f && num3 > num2)
			{
				num2 = num3;
				num = i;
			}
		}
		if (num != -1)
		{
			GameObject obj = CardChoice.instance.AddCard(cards[num]);
			obj.GetComponentInChildren<CardVisuals>().firstValueToSet = true;
			obj.transform.root.GetComponentInChildren<ApplyCardStats>().shootToPick = true;
		}
	}

	public static int GetClosestString(string inputText, string[] compareTo)
	{
		CardInfo[] cards = CardChoice.instance.cards;
		int result = -1;
		float num = 0f;
		for (int i = 0; i < cards.Length; i++)
		{
			string text = compareTo[i];
			text = text.Replace(" ", "");
			string text2 = inputText.ToUpper();
			text2 = text2.Replace(" ", "");
			float num2 = 0f;
			for (int j = 0; j < text2.Length; j++)
			{
				if (text.Length > j && text2[j] == text[j])
				{
					num2 += 1f / (float)text2.Length;
				}
			}
			num2 -= (float)Mathf.Abs(text2.Length - text.Length) * 0.001f;
			if (num2 > 0.1f && num2 > num)
			{
				num = num2;
				result = i;
			}
		}
		return result;
	}
}
