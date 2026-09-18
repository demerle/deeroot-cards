using Photon.Pun;
using UnityEngine;

public class LowFrameRate : MonoBehaviourPunCallbacks
{
	public enum SlowWhat
	{
		Both = 0,
		Server = 1,
		Client = 2
	}

	public SlowWhat slowWhat = SlowWhat.Server;

	public int targetFrameRate = 10;

	public override void OnJoinedRoom()
	{
		base.OnJoinedRoom();
		if (slowWhat == SlowWhat.Both || (PhotonNetwork.IsMasterClient && slowWhat == SlowWhat.Server) || (!PhotonNetwork.IsMasterClient && slowWhat == SlowWhat.Client))
		{
			Application.targetFrameRate = targetFrameRate;
			QualitySettings.vSyncCount = 0;
		}
		else
		{
			Application.targetFrameRate = 100;
		}
	}
}
