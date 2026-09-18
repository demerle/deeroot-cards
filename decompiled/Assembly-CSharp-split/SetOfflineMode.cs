using Photon.Pun;
using UnityEngine;

public class SetOfflineMode : MonoBehaviour
{
	public bool doIt = true;

	private void Awake()
	{
		if (doIt)
		{
			SetOffline();
		}
	}

	public void SetOffline()
	{
		PhotonNetwork.OfflineMode = true;
		PhotonNetwork.JoinRandomRoom();
	}

	public void SetOnline()
	{
		if (PhotonNetwork.InRoom)
		{
			PhotonNetwork.LeaveRoom();
		}
		PhotonNetwork.OfflineMode = false;
	}
}
