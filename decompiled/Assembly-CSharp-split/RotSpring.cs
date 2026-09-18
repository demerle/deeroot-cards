using UnityEngine;

public class RotSpring : MonoBehaviour
{
	public bool x;

	public bool y;

	public bool z;

	public float target;

	public float spring;

	public float damper;

	private float currentValue;

	private float vel;

	private void Start()
	{
		if (x)
		{
			currentValue = base.transform.localEulerAngles.x;
		}
		else if (y)
		{
			currentValue = base.transform.localEulerAngles.y;
		}
		else if (z)
		{
			currentValue = base.transform.localEulerAngles.z;
		}
	}

	private void Update()
	{
		vel = FRILerp.Lerp(vel, (target - currentValue) * spring, damper);
		currentValue += vel * TimeHandler.deltaTime;
		base.transform.localEulerAngles = new Vector3(x ? currentValue : 0f, y ? currentValue : 0f, z ? currentValue : 0f);
	}
}
