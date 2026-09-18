using UnityEngine;

public class FollowInactiveHand : MonoBehaviour
{
	private CharacterData data;

	public Vector3 offSet;

	public GameObject leftHand;

	public GameObject rightHand;

	private void Start()
	{
		data = base.transform.root.GetComponent<CharacterData>();
	}

	private void Update()
	{
		if (data.aimDirection.x < 0f)
		{
			base.transform.position = rightHand.transform.TransformPoint(offSet);
		}
		else
		{
			base.transform.position = leftHand.transform.TransformPoint(offSet);
		}
	}
}
