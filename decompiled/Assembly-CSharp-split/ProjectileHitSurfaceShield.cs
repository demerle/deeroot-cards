using UnityEngine;

public class ProjectileHitSurfaceShield : ProjectileHitSurface
{
	public override HasToStop HitSurface(HitInfo hit, GameObject projectile)
	{
		if (Vector3.Angle(base.transform.parent.forward, projectile.transform.forward) < 90f)
		{
			return HasToStop.HasToStop;
		}
		return HasToStop.CanKeepGoing;
	}
}
