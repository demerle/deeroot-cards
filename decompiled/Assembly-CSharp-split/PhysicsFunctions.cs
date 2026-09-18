using UnityEngine;

public class PhysicsFunctions : MonoBehaviour
{
	private static LayerMask mask = LayerMask.GetMask("Default", "IgnorePlayer", "IgnoreMap");

	public static Vector2 ObstructionPoint(Vector2 from, Vector2 to)
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(from, to - from, Vector2.Distance(from, to), mask);
		if ((bool)raycastHit2D.transform)
		{
			return raycastHit2D.point;
		}
		return to;
	}
}
