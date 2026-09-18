using UnityEngine;

public class RaycastForward : MonoBehaviour
{
	public LayerMask mask;

	public float distance = 100f;

	public RaycastHit2D hit;

	private void LateUpdate()
	{
		hit = Physics2D.Raycast(base.transform.position, base.transform.forward, distance, mask);
	}
}
