using UnityEngine;

public abstract class ProjectileHitSurface : MonoBehaviour
{
	public enum HasToStop
	{
		HasToStop,
		CanKeepGoing
	}

	public abstract HasToStop HitSurface(HitInfo hit, GameObject projectile);
}
