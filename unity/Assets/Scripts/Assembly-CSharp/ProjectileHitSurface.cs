using UnityEngine;

public abstract class ProjectileHitSurface : MonoBehaviour
{
	public enum HasToStop
	{
		HasToStop = 0,
		CanKeepGoing = 1
	}

	public abstract HasToStop HitSurface(HitInfo hit, GameObject projectile);
}
