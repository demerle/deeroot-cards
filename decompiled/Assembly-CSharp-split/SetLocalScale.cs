using UnityEngine;

public class SetLocalScale : MonoBehaviour
{
	public Vector3 localScale = Vector3.one;

	public void Set()
	{
		base.transform.localScale = localScale;
	}
}
