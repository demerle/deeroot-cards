using UnityEngine;

namespace Photon.Pun.Simple;

public class AutoOwnerGameObjEnable : MonoBehaviour, IOnAuthorityChanged
{
	public enum EnableIf
	{
		Owner,
		Other
	}

	public EnableIf enableIf;

	public void Start()
	{
		PhotonView componentInParent = GetComponentInParent<PhotonView>();
		if ((bool)componentInParent)
		{
			SwitchAuth(componentInParent.IsMine);
		}
	}

	public void OnAuthorityChanged(bool isMine, bool controllerChanged)
	{
		Debug.Log("AuthChanged");
		SwitchAuth(isMine);
	}

	private void SwitchAuth(bool isMine)
	{
		base.gameObject.SetActive((enableIf == EnableIf.Owner) ? isMine : (!isMine));
	}
}
