using UnityEngine;

public class RigLookUp : MonoBehaviour
{
	private Rigidbody2D rig;

	public float force;

	private void Start()
	{
		rig = GetComponent<Rigidbody2D>();
	}

	private void FixedUpdate()
	{
		rig.AddTorque(Vector3.Cross(base.transform.up, Vector3.up).normalized.z * force * rig.mass * Vector3.Angle(base.transform.up, Vector3.up), ForceMode2D.Force);
	}
}
