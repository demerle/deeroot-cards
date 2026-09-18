using UnityEngine;

public class PlayerAIPetter : MonoBehaviour
{
	private PlayerAPI api;

	public LayerMask m_layer;

	public AnimationCurve aimCurve;

	public float m_shootRandom = 0.9f;

	public float m_predDist = 1f;

	public float m_timeSinceGround = 0.1f;

	private BoxCollider2D m_collider;

	private void Start()
	{
		api = GetComponentInParent<PlayerAPI>();
		m_collider = api.GetComponentInChildren<BoxCollider2D>();
	}

	private void Update()
	{
		if ((double)Random.Range(0f, 1f) > 0.9)
		{
			api.Move(api.TowardsOtherPlayer() * -1f);
		}
		else
		{
			api.Move(api.TowardsOtherPlayer());
		}
		PredictionHit();
		api.Attack();
		if ((double)Random.Range(0f, 1f) > 0.9)
		{
			api.Jump();
		}
		AutoBlock();
	}

	private void AutoBlock()
	{
		foreach (BulletWrapper allBullet in api.GetAllBullets())
		{
			RaycastHit2D raycastHit2D = Physics2D.Raycast(allBullet.projectileMovement.transform.position, ((Vector2)allBullet.projectileMovement.velocity).normalized, allBullet.velocity.magnitude * 5f * TimeHandler.deltaTime, m_layer);
			if ((bool)raycastHit2D.transform && (!allBullet.projectileHit.ownPlayer || allBullet.projectileHit.ownPlayer != api.player) && raycastHit2D.transform.root == base.transform.root)
			{
				Debug.Log("BLICOK");
				api.Block();
			}
		}
	}

	private void PredictionHit()
	{
		if ((bool)api.GetOtherPlayer())
		{
			float magnitude = (api.OtherPlayerPosition() - base.transform.position).magnitude;
			Vector2 vector = api.GetOtherPlayer().data.playerVel.velocity * m_predDist * 0.1f * magnitude * 0.05f;
			api.SetAimDirection(api.TowardsOtherPlayer() + Vector2.up * aimCurve.Evaluate(magnitude) + vector);
		}
	}
}
