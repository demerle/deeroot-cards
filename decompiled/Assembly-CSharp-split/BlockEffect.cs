using UnityEngine;

public abstract class BlockEffect : MonoBehaviour
{
	public abstract void DoBlockedProjectile(GameObject projectile, Vector3 forward, Vector3 hitPos);
}
