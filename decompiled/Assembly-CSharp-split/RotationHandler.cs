using UnityEngine;

public class RotationHandler : MonoBehaviour
{
	private CharacterData data;

	private PlayerVelocity rig;

	public float torque;

	private void Start()
	{
		data = GetComponent<CharacterData>();
		rig = GetComponent<PlayerVelocity>();
	}

	private void FixedUpdate()
	{
		rig.AddTorque(torque * TimeHandler.timeScale * (Vector3.Angle(Vector3.up, base.transform.up) * Vector3.Cross(Vector3.up, base.transform.up).normalized).z);
	}
}
