using UnityEngine;

public class Thruster : MonoBehaviour
{
	public float force;

	public float drag;

	public float pow = 1f;

	public float physicsObjectM = 1f;

	private FollowLocalPos follow;

	private Rigidbody2D rig;

	private Vector2 startForward;

	private NetworkPhysicsObject pushed;

	private Player player;

	private bool checkedForPlayer;

	private void Start()
	{
		force *= Mathf.Pow(base.transform.localScale.x, pow);
		drag *= Mathf.Pow(base.transform.localScale.x, pow);
		follow = GetComponent<FollowLocalPos>();
		if ((bool)follow.target)
		{
			pushed = follow.target.gameObject.GetComponent<NetworkPhysicsObject>();
		}
		float num = 1f;
		if (!follow.targetPlayer)
		{
			num = 0.2f;
		}
		else if ((bool)pushed)
		{
			num = 0.5f;
		}
		ParticleSystem.MainModule main = GetComponentInChildren<ParticleSystem>().main;
		main.duration *= num;
		GetComponent<DelayEvent>().time *= num;
		GetComponent<RemoveAfterSeconds>().seconds *= num;
		startForward = base.transform.forward;
	}

	private void FixedUpdate()
	{
		if (!follow)
		{
			return;
		}
		if ((bool)follow.target)
		{
			if ((bool)player)
			{
				if ((bool)player)
				{
					player.data.healthHandler.TakeForce(startForward * force, ForceMode2D.Force);
				}
				else
				{
					rig.AddForce(startForward * force, ForceMode2D.Force);
				}
			}
			else if (!checkedForPlayer)
			{
				player = follow.target.transform.root.GetComponent<Player>();
				checkedForPlayer = true;
				if (!player && !pushed)
				{
					base.enabled = false;
				}
			}
		}
		if ((bool)pushed && pushed.photonView.IsMine)
		{
			pushed.RPCA_SendForce(base.transform.forward * force * physicsObjectM, pushed.transform.InverseTransformPoint(base.transform.position));
		}
	}
}
