using Photon.Pun;
using UnityEngine;

public class NetworkSettings : MonoBehaviour
{
	private void Start()
	{
		PhotonNetwork.SendRate = 30;
		PhotonNetwork.SerializationRate = 30;
	}
}
