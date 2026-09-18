using UnityEngine;

public class Movement : MonoBehaviour
{
	public float jumpForce;

	public float force;

	public float torque;

	public float drag = 1f;

	public float Angulardrag = 1f;

	public ParticleSystem[] jumpPart;

	private GeneralInput input;

	private Rigidbody2D rig;

	private CharacterData data;

	private CharacterStatModifiers stats;

	public Vector2 knockBack;

	private HealthHandler health;

	private Vector3 moveDir;

	private Vector3 idleForce;

	private void Start()
	{
		rig = GetComponent<Rigidbody2D>();
		input = GetComponent<GeneralInput>();
		data = GetComponent<CharacterData>();
		stats = GetComponent<CharacterStatModifiers>();
		health = GetComponent<HealthHandler>();
	}

	private void FixedUpdate()
	{
		if (input.direction != Vector3.zero)
		{
			moveDir = input.direction;
			idleForce = Vector3.Lerp(idleForce, moveDir, Time.fixedDeltaTime * 6f);
		}
		else if (data.isGrounded || data.isWallGrab)
		{
			idleForce = Vector3.Lerp(idleForce, Vector3.zero, 15f * Time.fixedDeltaTime);
		}
		else
		{
			idleForce = Vector3.Lerp(idleForce, Vector3.zero, 3f * Time.fixedDeltaTime);
		}
		if (input.direction == Vector3.zero)
		{
			moveDir = idleForce;
		}
		if (data.isGrounded || data.isWallGrab)
		{
			knockBack = Vector3.Lerp(knockBack, Vector3.zero, 10f * Time.fixedDeltaTime);
		}
		else
		{
			knockBack = Vector3.Lerp(knockBack, Vector3.zero, 7f * Time.fixedDeltaTime);
		}
		rig.AddForce(knockBack * 0.1f * rig.mass, ForceMode2D.Force);
		if (moveDir.y > 0f)
		{
			moveDir.y = 0f;
		}
		rig.AddForce(-rig.velocity * 0.01f * drag * rig.mass, ForceMode2D.Force);
		rig.AddTorque((0f - rig.angularVelocity) * 0.01f * Angulardrag * rig.mass, ForceMode2D.Force);
		if (input.jumpIsPressed)
		{
			rig.AddForce(Vector2.up * 20f * rig.mass, ForceMode2D.Force);
		}
		rig.AddForce(moveDir * force * (1f - stats.GetSlow()) * rig.mass, ForceMode2D.Force);
	}

	private void Update()
	{
		if (input.jumpWasPressed)
		{
			Jump();
		}
		if (data.isGrounded && data.sinceJump > 0.2f)
		{
			data.currentJumps = data.jumps;
		}
	}

	private void Jump()
	{
		if (data.sinceJump < 0.15f)
		{
			return;
		}
		Vector3 vector = Vector3.up;
		Vector3 vector2 = data.groundPos;
		if (data.sinceGrounded < 0.1f)
		{
			if (data.sinceGrounded > 0.05f)
			{
				vector2 = base.transform.position;
			}
			data.currentJumps = data.jumps;
		}
		else if (data.sinceWallGrab < 0.1f)
		{
			vector = Vector2.up + data.wallNormal;
			vector2 = data.wallPos;
			data.currentJumps = data.jumps;
		}
		else
		{
			if (data.currentJumps <= 0)
			{
				return;
			}
			vector2 = base.transform.position;
		}
		data.currentJumps--;
		rig.velocity = new Vector3(rig.velocity.x, 0f);
		health.TakeForce(Vector2.up * jumpForce * 1f * (1f - stats.GetSlow()) * rig.mass);
		data.sinceJump = 0f;
		data.sinceGrounded = 0f;
		for (int i = 0; i < jumpPart.Length; i++)
		{
			jumpPart[i].transform.position = new Vector3(vector2.x, vector2.y, 5f) - vector * 0f;
			jumpPart[i].transform.rotation = Quaternion.LookRotation(rig.velocity);
			jumpPart[i].Play();
		}
	}
}
