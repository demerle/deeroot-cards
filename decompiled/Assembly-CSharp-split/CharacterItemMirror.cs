using UnityEngine;

public class CharacterItemMirror : MonoBehaviour
{
	private float speedThreshol = 3f;

	private LeftRight leftRight;

	private Player player;

	private void Start()
	{
		player = GetComponentInParent<Player>();
		if (base.transform.localPosition.x > 0f)
		{
			leftRight = LeftRight.Right;
		}
		else
		{
			leftRight = LeftRight.Left;
		}
	}

	private void Update()
	{
		LeftRight leftRight = this.leftRight;
		if (player.data.playerVel.velocity.x > speedThreshol)
		{
			leftRight = LeftRight.Right;
		}
		if (player.data.playerVel.velocity.x < 0f - speedThreshol)
		{
			leftRight = LeftRight.Left;
		}
		if (leftRight != this.leftRight)
		{
			base.transform.localPosition = new Vector3(base.transform.localPosition.x * -1f, base.transform.localPosition.y, base.transform.localPosition.z);
			base.transform.localScale = new Vector3(base.transform.localScale.x * -1f, base.transform.localScale.y, base.transform.localScale.z);
			this.leftRight = leftRight;
		}
	}
}
