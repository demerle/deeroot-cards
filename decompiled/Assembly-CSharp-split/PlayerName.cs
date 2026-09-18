using Photon.Pun;
using TMPro;
using UnityEngine;

public class PlayerName : MonoBehaviour
{
	private void Start()
	{
		GetComponentInParent<TextMeshProUGUI>().text = GetComponentInParent<PhotonView>().Owner.NickName;
	}
}
