using UnityEngine;

public class Spring : MonoBehaviour
{
	public float spring = 1f;

	private Rigidbody2D rig;

	private Vector3 up;

	private void Start()
	{
		rig = GetComponent<Rigidbody2D>();
		up = base.transform.up;
	}

	private void FixedUpdate()
	{
		if ((bool)rig)
		{
			rig.AddTorque((Vector3.Cross(base.transform.up, up).normalized * Vector3.Angle(base.transform.up, up)).z * spring * rig.mass);
		}
	}
}
