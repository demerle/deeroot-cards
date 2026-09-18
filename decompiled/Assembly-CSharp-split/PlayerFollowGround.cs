using UnityEngine;

public class PlayerFollowGround : MonoBehaviour
{
	private CharacterData data;

	private Vector2 lastPos;

	private Rigidbody2D lastRig;

	private void Start()
	{
		data = GetComponent<CharacterData>();
	}

	private void FixedUpdate()
	{
		if (data.standOnRig == null || !data.isGrounded)
		{
			lastPos = Vector2.zero;
			return;
		}
		if (lastPos != Vector2.zero && data.standOnRig == lastRig)
		{
			data.playerVel.transform.position = data.playerVel.position + (data.standOnRig.position - lastPos);
		}
		lastPos = data.standOnRig.position;
		lastRig = data.standOnRig;
	}
}
