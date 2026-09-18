using UnityEngine;

public class ProjectileCollision : ProjectileHitSurface
{
	public bool scaleWithDMG;

	public float health;

	private float deathThreshold;

	private RayHitReflect reflect;

	private ChildRPC rpc;

	private float startDMG;

	private bool hasCollided;

	public GameObject sparkObject;

	private void Start()
	{
		rpc = GetComponentInParent<ChildRPC>();
		rpc.childRPCs.Add("KillBullet", Die);
		reflect = GetComponentInParent<RayHitReflect>();
		ProjectileHit componentInParent = GetComponentInParent<ProjectileHit>();
		if (scaleWithDMG)
		{
			base.transform.localScale *= (componentInParent.damage / 55f + 1f) * 0.5f;
			health = componentInParent.damage;
		}
		startDMG = componentInParent.damage;
		deathThreshold = health * 0.1f;
	}

	public override HasToStop HitSurface(HitInfo hit, GameObject projectile)
	{
		if (Vector2.Angle(base.transform.root.forward, projectile.transform.forward) < 45f)
		{
			return HasToStop.HasToStop;
		}
		if ((bool)reflect && reflect.timeOfBounce + 0.5f > Time.time)
		{
			return HasToStop.HasToStop;
		}
		ProjectileCollision componentInChildren = projectile.GetComponentInChildren<ProjectileCollision>();
		if ((bool)componentInChildren)
		{
			reflect = componentInChildren.GetComponentInParent<RayHitReflect>();
			if ((bool)reflect && reflect.timeOfBounce + 0.5f > Time.time)
			{
				return HasToStop.HasToStop;
			}
			float dmg = health;
			float dmg2 = componentInChildren.health;
			componentInChildren.TakeDamage(dmg);
			TakeDamage(dmg2);
		}
		return HasToStop.HasToStop;
	}

	public void TakeDamage(float dmg)
	{
		if (!hasCollided)
		{
			health -= dmg;
			if ((bool)rpc && health < deathThreshold)
			{
				rpc.CallFunction("KillBullet");
			}
		}
	}

	public void Die()
	{
		if (!hasCollided)
		{
			hasCollided = true;
			RaycastHit2D raycastHit2D = new RaycastHit2D
			{
				normal = -base.transform.root.forward,
				point = base.transform.position
			};
			Object.Instantiate(sparkObject, base.transform.position, base.transform.rotation).transform.localScale = Vector3.one * ((startDMG / 55f + 1f) * 0.5f);
			GetComponentInParent<ProjectileHit>().Hit(HitInfo.GetHitInfo(raycastHit2D), forceCall: true);
		}
	}
}
