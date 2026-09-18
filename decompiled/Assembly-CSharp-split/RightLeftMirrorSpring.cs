using UnityEngine;

public class RightLeftMirrorSpring : MonoBehaviour
{
	public Vector3 leftPos;

	private Vector3 rightPos;

	public float leftRot;

	public float rightRot;

	private Vector3 posVel;

	private float rotVel;

	public float drag = 25f;

	public float spring = 25f;

	private Holdable holdable;

	private float currentRot;

	private void Start()
	{
		currentRot = base.transform.localEulerAngles.z;
		holdable = base.transform.root.GetComponent<Holdable>();
		rightPos = base.transform.localPosition;
	}

	private void Update()
	{
		if ((bool)holdable && (bool)holdable.holder)
		{
			bool num = base.transform.root.position.x - 0.1f < holdable.holder.transform.position.x;
			Vector3 vector = (num ? leftPos : rightPos);
			float num2 = (num ? leftRot : rightRot);
			posVel = FRILerp.Lerp(posVel, (vector - base.transform.localPosition) * spring, drag);
			rotVel = FRILerp.Lerp(rotVel, (num2 - currentRot) * spring, drag);
			currentRot += rotVel * TimeHandler.deltaTime;
			base.transform.localPosition += posVel * TimeHandler.deltaTime;
			base.transform.localEulerAngles = new Vector3(0f, 0f, currentRot);
		}
	}
}
