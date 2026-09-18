using UnityEngine;

public class RunSmoke : MonoBehaviour
{
	private CharacterData data;

	private ParticleSystem part;

	private float c;

	private bool groundedLastFrame;

	private ParticleSystem.MainModule partVel;

	private bool didTryToEmit;

	private Vector3 vel;

	private void Start()
	{
		part = GetComponent<ParticleSystem>();
		data = GetComponentInParent<CharacterData>();
		partVel = part.main;
	}

	private void Update()
	{
		didTryToEmit = false;
		if (data.isGrounded && Mathf.Abs(data.playerVel.velocity.x) > 5f)
		{
			base.transform.position = new Vector3(data.transform.position.x, data.groundPos.y, 5f);
			Go();
		}
		else if (data.isWallGrab && data.wallDistance < 0.7f && data.playerVel.velocity.magnitude > 5f)
		{
			base.transform.position = new Vector3(data.wallPos.x, data.transform.position.y, 5f);
			Go();
		}
		if (didTryToEmit)
		{
			vel = Vector3.Lerp(vel, data.playerVel.velocity, TimeHandler.deltaTime * 2f);
		}
		else
		{
			vel = Vector3.Lerp(vel, Vector3.zero, TimeHandler.deltaTime * 10f);
		}
	}

	private void Go()
	{
		didTryToEmit = true;
		c += TimeHandler.deltaTime;
		if (c > 0.02f)
		{
			c = 0f;
			float num = 2f;
			part.transform.rotation = Quaternion.LookRotation(vel);
			partVel.startSpeedMultiplier = data.playerVel.velocity.magnitude * num;
			part.Emit(2);
		}
	}
}
