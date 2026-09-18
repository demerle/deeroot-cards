using UnityEngine;

public class ArmRotator : MonoBehaviour
{
	public Vector3 rotation;

	private void Start()
	{
		base.transform.localEulerAngles = rotation;
	}
}
