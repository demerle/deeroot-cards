using UnityEngine;

public class DisableIfSimple : MonoBehaviour
{
	private void Start()
	{
		Unparent componentInParent = GetComponentInParent<Unparent>();
		if ((bool)componentInParent && (bool)componentInParent.parent)
		{
			base.gameObject.SetActive(!componentInParent.parent.root.GetComponentInChildren<PlayerSkinHandler>().simpleSkin);
		}
		else
		{
			base.gameObject.SetActive(!base.transform.root.GetComponentInChildren<PlayerSkinHandler>().simpleSkin);
		}
	}
}
