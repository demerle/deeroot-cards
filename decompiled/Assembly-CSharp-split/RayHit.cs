using UnityEngine;

public abstract class RayHit : MonoBehaviour
{
	public abstract void Hit(HitInfo hit, bool forceCall = false);
}
