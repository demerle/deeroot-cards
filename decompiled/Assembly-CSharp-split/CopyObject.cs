using UnityEngine;

public class CopyObject : MonoBehaviour
{
	public void CopySelf()
	{
		Object.Instantiate(base.gameObject, base.transform.position, base.transform.rotation, base.transform.parent);
	}
}
