using UnityEngine;

public class VelocityStretch : MonoBehaviour
{
	private Rigidbody2D rig;

	public float amount = 1f;

	private void Start()
	{
		rig = GetComponentInParent<Rigidbody2D>();
	}

	private void Update()
	{
		base.transform.localScale = Vector3.one + new Vector3(Mathf.Abs(rig.velocity.x), Mathf.Abs(rig.velocity.y), 0f) * amount;
	}
}
