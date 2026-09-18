using UnityEngine;

public class FollowLocalPos : MonoBehaviour
{
	private Vector3 relative;

	private Vector3 relativeForward;

	private Vector3 relativeUp;

	public Transform target;

	public Player targetPlayer;

	private void Start()
	{
		if ((bool)target && relative == Vector3.zero)
		{
			Follow(target);
		}
	}

	private void LateUpdate()
	{
		if ((bool)target && target.gameObject.activeInHierarchy)
		{
			base.transform.position = target.TransformPoint(relative);
			base.transform.rotation = Quaternion.LookRotation(target.TransformDirection(relativeForward), target.TransformDirection(relativeUp));
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}

	public void Follow(Transform targetTransform)
	{
		if ((bool)targetTransform)
		{
			target = targetTransform;
			Player component = target.transform.root.GetComponent<Player>();
			Vector3 position = base.transform.position;
			if ((bool)component)
			{
				targetPlayer = component;
				base.transform.position -= base.transform.forward * 2f;
				Vector3 position2 = target.position;
				Vector3 vector = (base.transform.position - target.position).normalized * 1.1f;
				position = position2 + vector;
				base.transform.position = new Vector3(base.transform.position.x, base.transform.position.y, component.transform.position.z);
			}
			relative = targetTransform.InverseTransformPoint(position);
			relativeForward = targetTransform.InverseTransformDirection(base.transform.forward);
			relativeUp = targetTransform.InverseTransformDirection(base.transform.up);
		}
	}
}
