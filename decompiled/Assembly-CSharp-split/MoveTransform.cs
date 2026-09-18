using UnityEngine;

public class MoveTransform : MonoBehaviour
{
	public float gravity = 30f;

	public float drag;

	public float dragMinSpeed = 1f;

	public float velocitySpread;

	public float spread;

	public Vector3 localForce;

	public Vector3 worldForce;

	public float multiplier = 1f;

	public Vector3 velocity;

	[HideInInspector]
	public float distanceTravelled;

	[HideInInspector]
	public bool DontRunStart;

	public float selectedSpread;

	public bool allowStop;

	public int simulateGravity;

	private int randomSeed;

	[HideInInspector]
	internal float simulationSpeed = 1f;

	private void Start()
	{
		if (!DontRunStart)
		{
			velocity += base.transform.TransformDirection(localForce) + worldForce;
			base.transform.rotation = Quaternion.LookRotation(velocity, Vector3.forward);
			if (spread != 0f)
			{
				velocity += base.transform.up * selectedSpread;
			}
		}
	}

	private void Update()
	{
		float num = Mathf.Clamp(TimeHandler.deltaTime, 0f, 0.02f);
		float deltaTime = TimeHandler.deltaTime;
		num *= simulationSpeed;
		deltaTime *= simulationSpeed;
		if (simulateGravity == 0)
		{
			velocity += gravity * Vector3.down * deltaTime * multiplier;
		}
		if ((velocity.magnitude > 2f || allowStop) && velocity.magnitude > dragMinSpeed)
		{
			velocity -= velocity * Mathf.Clamp(drag * num * Mathf.Clamp(multiplier, 0f, 1f), 0f, 1f);
		}
		base.transform.position += velocity * deltaTime * multiplier;
		distanceTravelled += velocity.magnitude * deltaTime * multiplier;
		base.transform.rotation = Quaternion.LookRotation(velocity, Vector3.forward);
	}

	public float GetUpwardsCompensation(Vector2 start, Vector2 end)
	{
		start.y = 0.5f;
		end.y = 0.5f;
		return Mathf.Pow(Vector3.Distance(start, end), 2.06f) * gravity / velocity.magnitude * 0.012f;
	}
}
