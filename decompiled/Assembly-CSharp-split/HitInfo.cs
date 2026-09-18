using UnityEngine;

public class HitInfo
{
	public Vector2 point;

	public Vector2 normal;

	public Collider2D collider;

	public Transform transform;

	public Rigidbody2D rigidbody;

	internal static HitInfo GetHitInfo(RaycastHit2D raycastHit2D)
	{
		return new HitInfo
		{
			point = raycastHit2D.point,
			normal = raycastHit2D.normal,
			transform = raycastHit2D.transform,
			collider = raycastHit2D.collider,
			rigidbody = raycastHit2D.rigidbody
		};
	}
}
