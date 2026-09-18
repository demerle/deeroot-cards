using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	public float force;

	public float airControl = 0.3f;

	public float extraDrag;

	public float extraAngularDrag;

	public float wallGrabDrag;

	private CharacterData data;

	private CharacterStatModifiers stats;

	private float multiplier = 1f;

	private void Start()
	{
		data = GetComponent<CharacterData>();
		stats = GetComponent<CharacterStatModifiers>();
	}

	private void FixedUpdate()
	{
		if (!data.isPlaying)
		{
			return;
		}
		Move(data.input.direction);
		if (data.isWallGrab && data.wallDistance < 0.7f)
		{
			Vector2 velocity = data.playerVel.velocity;
			if (data.input.direction.y >= 0f)
			{
				_ = data.input.direction.x;
				_ = 0f;
			}
			data.playerVel.velocity = velocity;
		}
		data.playerVel.velocity -= data.playerVel.velocity * TimeHandler.timeScale * 0.01f * 0.1f * extraDrag * multiplier;
		data.playerVel.angularVelocity -= data.playerVel.angularVelocity * TimeHandler.timeScale * 0.01f * 0.1f * extraAngularDrag * multiplier;
	}

	private void Update()
	{
	}

	public void Move(Vector2 direction)
	{
		UpdateMultiplier();
		if (!data.isStunned)
		{
			direction.y = Mathf.Clamp(direction.y, -1f, 0f);
			direction.y *= 2f;
			data.playerVel.AddForce(direction * TimeHandler.timeScale * (1f - stats.GetSlow()) * stats.movementSpeed * force * data.playerVel.mass * 0.01f * multiplier, ForceMode2D.Force);
		}
	}

	private void UpdateMultiplier()
	{
		multiplier = 1f;
		if (!data.isGrounded)
		{
			multiplier = airControl;
		}
	}
}
