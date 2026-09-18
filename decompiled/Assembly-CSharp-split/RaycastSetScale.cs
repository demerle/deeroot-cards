using UnityEngine;

public class RaycastSetScale : MonoBehaviour
{
	private RaycastForward raycast;

	private void Start()
	{
		raycast = GetComponent<RaycastForward>();
	}

	private void LateUpdate()
	{
		if ((bool)raycast.hit.transform)
		{
			base.transform.localScale = new Vector3(base.transform.localScale.x, base.transform.localScale.y, raycast.hit.distance);
		}
		else
		{
			base.transform.localScale = new Vector3(base.transform.localScale.x, base.transform.localScale.y, raycast.distance);
		}
	}
}
