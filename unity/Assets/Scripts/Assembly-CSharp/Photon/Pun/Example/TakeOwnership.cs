using UnityEngine;

namespace Photon.Pun.Example
{
	public class TakeOwnership : MonoBehaviour
	{
		public KeyCode keycode = KeyCode.C;

		private void Start()
		{
		}

		private void Update()
		{
			if (Input.GetKeyDown(keycode))
			{
				TransferOwner();
			}
		}

		public void TransferOwner()
		{
			GetComponent<PhotonView>().TransferOwnership(PhotonNetwork.LocalPlayer.ActorNumber);
		}
	}
}
