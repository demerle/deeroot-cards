using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ChildRPC : MonoBehaviour
{
	public Dictionary<string, Action<Vector2, Vector2, int>> childRPCsVector2Vector2Int = new Dictionary<string, Action<Vector2, Vector2, int>>();

	public Dictionary<string, Action<Vector2, Vector2, int, int>> childRPCsVector2Vector2IntInt = new Dictionary<string, Action<Vector2, Vector2, int, int>>();

	public Dictionary<string, Action<Vector2>> childRPCsVector2 = new Dictionary<string, Action<Vector2>>();

	public Dictionary<string, Action<Vector3, Quaternion>> childRPCsVector3Quaternion = new Dictionary<string, Action<Vector3, Quaternion>>();

	public Dictionary<string, Action<int>> childRPCsInt = new Dictionary<string, Action<int>>();

	public Dictionary<string, Action> childRPCs = new Dictionary<string, Action>();

	private PhotonView view;

	private void Start()
	{
		view = GetComponent<PhotonView>();
	}

	public void CallFunction(string key)
	{
		view.RPC("RPCA_RecieveFunction", RpcTarget.All, key);
	}

	[PunRPC]
	public void RPCA_RecieveFunction(string key)
	{
		if (childRPCs.ContainsKey(key))
		{
			childRPCs[key]();
		}
	}

	public void CallFunction(string key, int intData)
	{
		view.RPC("RPCA_RecieveFunction", RpcTarget.All, key, intData);
	}

	[PunRPC]
	public void RPCA_RecieveFunction(string key, int intData)
	{
		if (childRPCsInt.ContainsKey(key))
		{
			childRPCsInt[key](intData);
		}
	}

	public void CallFunction(string key, Vector2 vectorData)
	{
		view.RPC("RPCA_RecieveFunction", RpcTarget.All, key, vectorData);
	}

	[PunRPC]
	public void RPCA_RecieveFunction(string key, Vector2 vectorData)
	{
		if (childRPCsVector2.ContainsKey(key))
		{
			childRPCsVector2[key](vectorData);
		}
	}

	public void CallFunction(string key, Vector2 vectorData, Vector2 vectorData2, int intData)
	{
		view.RPC("RPCA_RecieveFunction", RpcTarget.All, key, vectorData, vectorData2, intData);
	}

	[PunRPC]
	public void RPCA_RecieveFunction(string key, Vector2 vectorData, Vector2 vectorData2, int intData)
	{
		if (childRPCsVector2Vector2Int.ContainsKey(key))
		{
			childRPCsVector2Vector2Int[key](vectorData, vectorData2, intData);
		}
	}

	public void CallFunction(string key, Vector2 vectorData, Vector2 vectorData2, int intData, int intData2)
	{
		view.RPC("RPCA_RecieveFunction", RpcTarget.All, key, vectorData, vectorData2, intData, intData2);
	}

	[PunRPC]
	public void RPCA_RecieveFunction(string key, Vector2 vectorData, Vector2 vectorData2, int intData, int intData2)
	{
		if (childRPCsVector2Vector2IntInt.ContainsKey(key))
		{
			childRPCsVector2Vector2IntInt[key](vectorData, vectorData2, intData, intData2);
		}
	}

	public void CallFunction(string key, Vector3 vectorData, Quaternion quaterion)
	{
		view.RPC("RPCA_RecieveFunction", RpcTarget.All, key, vectorData, quaterion);
	}

	[PunRPC]
	public void RPCA_RecieveFunction(string key, Vector3 vectorData, Quaternion quaterion)
	{
		if (childRPCsVector3Quaternion.ContainsKey(key))
		{
			childRPCsVector3Quaternion[key](vectorData, quaterion);
		}
	}
}
