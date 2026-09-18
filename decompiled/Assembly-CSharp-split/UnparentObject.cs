using UnityEngine;

public class UnparentObject : MonoBehaviour
{
	public void Unparent()
	{
		base.transform.SetParent(base.transform.root);
	}
}
