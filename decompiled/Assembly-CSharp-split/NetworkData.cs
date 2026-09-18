using Photon.Pun;
using UnityEngine;

public class NetworkData : MonoBehaviour
{
	private PhotonView photonView;

	private bool inited;

	private void Start()
	{
		photonView = GetComponent<PhotonView>();
	}

	private void Init()
	{
		if (!inited)
		{
			inited = true;
			if (PhotonNetwork.IsMasterClient)
			{
				Debug.Log("Why am i the master?");
			}
		}
	}

	private void Update()
	{
		if (PhotonNetwork.InRoom)
		{
			Init();
		}
	}

	private void RequestJoin()
	{
		photonView.RPC("RequestJoinMaster", RpcTarget.MasterClient);
		Debug.Log("Request join");
	}

	[PunRPC]
	public void RequestJoinMaster()
	{
		string text = JsonUtility.ToJson(new InitPackage
		{
			currentMapID = MapManager.instance.currentLevelID
		});
		photonView.RPC("RequestJoinResponse", RpcTarget.Others, text);
	}

	[PunRPC]
	public void RequestJoinResponse(string jsonResponse)
	{
		InitPackage initPackage = (InitPackage)JsonUtility.FromJson(jsonResponse, typeof(InitPackage));
		MapManager.instance.LoadLevelFromID(initPackage.currentMapID, onlyMaster: false, callInImidetly: true);
		Debug.Log("Got response");
	}
}
