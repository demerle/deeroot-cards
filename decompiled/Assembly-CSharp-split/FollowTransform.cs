using UnityEngine;

public class FollowTransform : MonoBehaviour
{
	public Transform target;

	private Vector3 spawnOffset;

	public Vector3 offset;

	private void Start()
	{
		spawnOffset = base.transform.position - target.position;
	}

	private void LateUpdate()
	{
		if ((bool)target)
		{
			base.transform.position = target.position + spawnOffset + offset;
		}
	}
}
