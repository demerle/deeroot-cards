using UnityEngine;

namespace Photon.Pun.Simple;

public class EnableOnNetObjReady : MonoBehaviour, IOnNetObjReady
{
	public GameObject visibilityObject;

	public void Reset()
	{
		visibilityObject = base.gameObject;
	}

	public void Awake()
	{
		if (visibilityObject == null)
		{
			visibilityObject = base.gameObject;
		}
	}

	private void Start()
	{
		NetObject componentInParent = GetComponentInParent<NetObject>();
		if ((bool)componentInParent)
		{
			visibilityObject.SetActive(componentInParent.AllObjsAreReady);
		}
	}

	public void OnNetObjReadyChange(bool ready)
	{
		if (visibilityObject == null)
		{
			visibilityObject = base.gameObject;
		}
		visibilityObject.SetActive(ready);
	}
}
