using System;
using UnityEngine;

public class PlayerJump : MonoBehaviour
{
	private CharacterData data;

	public float upForce;

	private CharacterStatModifiers stats;

	public ParticleSystem[] jumpPart;

	public float sideForce = 1f;

	public Action JumpAction;

	private void Start()
	{
		stats = GetComponent<CharacterStatModifiers>();
		data = GetComponent<CharacterData>();
	}

	private void Update()
	{
		if (data.input.jumpWasPressed)
		{
			Jump();
		}
		if (data.input.jumpIsPressed && data.sinceJump < 0.2f)
		{
			data.playerVel.AddForce(Vector2.up * TimeHandler.deltaTime * 2f * data.stats.jump * data.playerVel.mass * (1f - stats.GetSlow()) * upForce, ForceMode2D.Force);
		}
	}

	public void Jump(bool forceJump = false, float multiplier = 1f)
	{
		if (!forceJump && (data.sinceJump < 0.1f || (data.currentJumps <= 0 && data.sinceWallGrab > 0.1f)))
		{
			return;
		}
		Vector3 vector = Vector3.up;
		Vector3 vector2 = data.groundPos;
		if (JumpAction != null)
		{
			JumpAction();
		}
		bool flag = false;
		if (data.sinceWallGrab < 0.1f && !data.isGrounded)
		{
			vector = Vector2.up * 0.8f + data.wallNormal * 0.4f;
			vector2 = data.wallPos;
			data.currentJumps = data.jumps;
			flag = true;
		}
		else
		{
			if (data.sinceGrounded > 0.05f)
			{
				vector2 = base.transform.position;
			}
			data.currentJumps = data.jumps;
		}
		if (data.playerVel.velocity.y < 0f)
		{
			data.playerVel.velocity = new Vector2(data.playerVel.velocity.x, 0f);
		}
		data.sinceGrounded = 0f;
		data.sinceJump = 0f;
		data.isGrounded = false;
		data.isWallGrab = false;
		data.currentJumps--;
		data.playerVel.AddForce(vector * multiplier * 0.01f * data.stats.jump * data.playerVel.mass * (1f - stats.GetSlow()) * upForce, ForceMode2D.Impulse);
		if (!flag)
		{
			data.playerVel.AddForce(Vector2.right * multiplier * sideForce * 0.01f * data.stats.jump * data.playerVel.mass * (1f - stats.GetSlow()) * data.playerVel.velocity.x, ForceMode2D.Impulse);
		}
		for (int i = 0; i < jumpPart.Length; i++)
		{
			jumpPart[i].transform.position = new Vector3(vector2.x, vector2.y, 5f) - vector * 0f;
			jumpPart[i].transform.rotation = Quaternion.LookRotation(data.playerVel.velocity);
			jumpPart[i].Play();
		}
	}
}
