using UnityEngine;

public class FollowScale : MonoBehaviour
{
	public Transform target;

	private void Update()
	{
		if ((bool)target)
		{
			base.transform.localScale = target.transform.localScale;
		}
	}
}
