using UnityEngine;

public abstract class RayHitEffect : MonoBehaviour
{
	public int priority;

	public abstract HasToReturn DoHitEffect(HitInfo hit);
}
