using System.Collections;
using Photon.Pun;
using Sonigon;
using UnityEngine;

public class BounceEffectRetarget : BounceEffect
{
	[Header("Sound")]
	public SoundEvent soundTargetBounceTargetPlayer;

	private MoveTransform move;

	private PhotonView view;

	private void Start()
	{
		view = GetComponentInParent<PhotonView>();
		move = GetComponentInParent<MoveTransform>();
		GetComponentInParent<ChildRPC>().childRPCsVector2.Add("TargetBounce", SetNewVel);
		GetComponentInParent<ChildRPC>().childRPCsInt.Add("TargetBounceLine", DrawLineTo);
	}

	public override void DoBounce(HitInfo hit)
	{
		StartCoroutine(DelayMove(hit));
	}

	private void ActuallyDoBounce(int playerId)
	{
		Player playerWithID = PlayerManager.instance.GetPlayerWithID(playerId);
		if ((bool)playerWithID)
		{
			GetComponentInParent<ChildRPC>().CallFunction("TargetBounce", (playerWithID.data.playerVel.position + Vector2.up * move.GetUpwardsCompensation(base.transform.position, playerWithID.data.playerVel.position) - (Vector2)base.transform.position).normalized * move.velocity.magnitude);
			SoundManager.Instance.PlayAtPosition(soundTargetBounceTargetPlayer, SoundManager.Instance.GetTransform(), base.transform);
		}
		else
		{
			GetComponentInParent<ChildRPC>().CallFunction("TargetBounce", move.velocity);
		}
	}

	private void SetNewVel(Vector2 newVel)
	{
		move.enabled = true;
		move.velocity = newVel;
	}

	private Player FindTarget(HitInfo hit)
	{
		Player closestPlayer = PlayerManager.instance.GetClosestPlayer(base.transform.position + (Vector3)hit.normal * 0.1f);
		if (PlayerManager.instance.CanSeePlayer(base.transform.position, closestPlayer).canSee)
		{
			return closestPlayer;
		}
		return null;
	}

	private IEnumerator DelayMove(HitInfo hit)
	{
		Player p = FindTarget(hit);
		if ((bool)p && view.IsMine)
		{
			GetComponentInParent<ChildRPC>().CallFunction("TargetBounceLine", p.playerID);
		}
		move.enabled = false;
		if ((bool)hit.rigidbody)
		{
			move.GetComponent<RayCastTrail>().IgnoreRigFor(hit.rigidbody, 0.5f);
		}
		yield return new WaitForSeconds(0.1f);
		if (view.IsMine)
		{
			if ((bool)p)
			{
				ActuallyDoBounce(p.playerID);
			}
			else
			{
				ActuallyDoBounce(-1);
			}
		}
	}

	private void DrawLineTo(int playerID)
	{
		Player playerWithID = PlayerManager.instance.GetPlayerWithID(playerID);
		if ((bool)playerWithID)
		{
			StartCoroutine(DrawLine(playerWithID.transform));
		}
	}

	private IEnumerator DrawLine(Transform target)
	{
		LineEffect line = GetComponentInChildren<LineEffect>(includeInactive: true);
		line.StartDraw();
		while ((bool)line)
		{
			line.DrawLine(base.transform.position, target.position);
			yield return null;
		}
	}
}
