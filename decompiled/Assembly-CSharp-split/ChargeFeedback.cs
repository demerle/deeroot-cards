using UnityEngine;

public class ChargeFeedback : MonoBehaviour
{
	private Gun gun;

	public float drag = 1f;

	public float spring = 1f;

	public float angle = 45f;

	public float chargeAngle = 15f;

	private float currentAngle;

	private float velocity;

	public float charge;

	private void Start()
	{
		gun = GetComponentInParent<Gun>();
		gun.AddAttackAction(Shoot);
	}

	private void Update()
	{
		charge = 0f;
		if (!gun.IsReady(0.15f))
		{
			charge = 1f;
		}
		velocity += (charge * chargeAngle - currentAngle) * CappedDeltaTime.time * spring;
		velocity -= velocity * CappedDeltaTime.time * drag;
		currentAngle += CappedDeltaTime.time * velocity;
		base.transform.localEulerAngles = new Vector3(currentAngle, 0f, 0f);
	}

	public void Shoot()
	{
		velocity += (0f - angle) * 1000f * (gun.damage / 55f);
	}
}
