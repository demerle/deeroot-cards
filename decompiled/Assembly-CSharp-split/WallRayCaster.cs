using UnityEngine;

public class WallRayCaster : MonoBehaviour
{
	public float rayLength = 0.7f;

	public LayerMask mask;

	private GeneralInput input;

	private CharacterData data;

	private Rigidbody2D rig;

	private void Start()
	{
		input = GetComponent<GeneralInput>();
		data = GetComponent<CharacterData>();
		rig = GetComponent<Rigidbody2D>();
	}

	private void Update()
	{
	}

	public void RayCast(Vector3 dir, float offset = 0f)
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position + base.transform.up * offset, dir, rayLength * base.transform.localScale.x, mask);
		if ((bool)raycastHit2D.transform && !raycastHit2D.collider.GetComponent<DamageBox>() && Vector3.Angle(raycastHit2D.normal, Vector3.up) > 70f && Vector3.Angle(raycastHit2D.normal, Vector3.up) < 110f)
		{
			data.TouchWall(raycastHit2D.normal, raycastHit2D.point);
		}
	}
}
