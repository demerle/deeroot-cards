using UnityEngine;

public class PlayerAIMinion : MonoBehaviour
{
	public AnimationCurve m_AimCompensastionCurve;

	public float range = 5f;

	private PlayerAPI api;

	private Vector2 moveDirection;

	private void Start()
	{
		api = GetComponentInParent<PlayerAPI>();
		moveDirection = api.data.master.data.aimDirection;
		if (moveDirection.x > 0.5f)
		{
			moveDirection.x = 1f;
		}
		else if (moveDirection.x < -0.5f)
		{
			moveDirection.x = -1f;
		}
		else
		{
			moveDirection.x = 0f;
		}
		moveDirection.y = 0f;
		base.transform.root.gameObject.AddComponent<RemoveAfterSeconds>().seconds = 4f;
	}

	private void Update()
	{
		Player otherPlayer = PlayerManager.instance.GetOtherPlayer(api.data.master);
		api.Move(moveDirection);
		if (api.data.isWallGrab)
		{
			api.Jump();
		}
		if (!api.data.isGrounded)
		{
			api.Jump();
		}
		if ((bool)otherPlayer)
		{
			api.SetAimDirection(GetAimDirForHitting(otherPlayer.transform.position));
			if (PlayerManager.instance.CanSeePlayer(base.transform.position, otherPlayer).canSee && Vector2.Distance(base.transform.position, otherPlayer.transform.position) < range)
			{
				api.Attack();
			}
		}
	}

	private Vector2 GetAimDirForHitting(Vector3 point)
	{
		Vector3 vector = point - base.transform.position;
		api.SetAimDirection(vector);
		api.GetMyBullet();
		float time = Mathf.Abs(point.x - base.transform.position.x);
		return point + Vector3.up * m_AimCompensastionCurve.Evaluate(time) - base.transform.position;
	}
}
