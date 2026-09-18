using System;
using Photon.Pun;
using UnityEngine;

public class Implosion : MonoBehaviour
{
	public float force;

	public float drag;

	public float time;

	public float clampDist;

	private void Start()
	{
		Explosion component = GetComponent<Explosion>();
		component.HitTargetAction = (Action<Damagable, float>)Delegate.Combine(component.HitTargetAction, new Action<Damagable, float>(HitTarget));
		clampDist *= base.transform.localScale.x;
		force *= base.transform.localScale.x;
	}

	public void HitTarget(Damagable damageble, float distance)
	{
		DoPull(damageble, distance);
	}

	private void DoPull(Damagable damageble, float distance)
	{
		bool num = GetComponent<SpawnedAttack>().IsMine();
		HealthHandler component = damageble.GetComponent<HealthHandler>();
		CharacterData component2 = damageble.GetComponent<CharacterData>();
		Vector2 vector = (Vector2)((base.transform.position - component.transform.position) * 0.25f);
		if (num)
		{
			component2.view.RPC("RPCA_SendForceTowardsPointOverTime", RpcTarget.All, force, drag, clampDist, (Vector2)base.transform.position, time, 0, false, false);
		}
	}
}
