using System;
using UnityEngine;

public class LevelMapper : MonoBehaviour
{
	private CharacterData data;

	private PlayerJump jump;

	private PlayerMovement movement;

	private bool isJumping;

	private bool isRunning;

	public GameObject node;

	public GameObject line;

	public GameObject line2;

	private Vector3 jumpPos;

	private Vector3 landPos;

	private Vector3 startRunPos;

	private Vector3 leaveGroundPos;

	private void Start()
	{
		data = GetComponentInParent<CharacterData>();
		jump = data.jump;
		movement = data.movement;
		PlayerJump playerJump = jump;
		playerJump.JumpAction = (Action)Delegate.Combine(playerJump.JumpAction, new Action(Jump));
		CharacterData characterData = data;
		characterData.TouchGroundAction = (Action<float, Vector3, Vector3, Transform>)Delegate.Combine(characterData.TouchGroundAction, new Action<float, Vector3, Vector3, Transform>(Ground));
		CharacterData characterData2 = data;
		characterData2.TouchWallAction = (Action<float, Vector3, Vector3>)Delegate.Combine(characterData2.TouchWallAction, new Action<float, Vector3, Vector3>(Wall));
	}

	private void Update()
	{
		if (!data.isGrounded)
		{
			LeaveGround();
		}
	}

	private void Jump()
	{
		isJumping = true;
		jumpPos = base.transform.position;
	}

	private void Wall(float sinceWall, Vector3 pos, Vector3 normal)
	{
		if (!data.isWallGrab)
		{
			if (data.sinceJump < 0.1f)
			{
				isJumping = false;
			}
			else
			{
				Land();
			}
		}
	}

	private void Ground(float sinceGround, Vector3 pos, Vector3 groundNormal, Transform groundTransform = null)
	{
		if (!isRunning)
		{
			startRunPos = pos;
			isRunning = true;
		}
		if (!data.isGrounded)
		{
			if (data.sinceJump < 0.1f)
			{
				isJumping = false;
			}
			else
			{
				Land();
			}
		}
	}

	private void Land()
	{
		landPos = base.transform.position;
		if (Vector3.Distance(landPos, jumpPos) > 3f && isJumping)
		{
			SaveJump();
		}
		isJumping = false;
	}

	private void LeaveGround()
	{
		if (isRunning)
		{
			leaveGroundPos = data.groundPos;
			SaveRun();
			isRunning = false;
		}
	}

	private void SaveJump()
	{
		UnityEngine.Object.Instantiate(node, jumpPos, Quaternion.identity).GetComponent<SpriteRenderer>().color = Color.green;
		UnityEngine.Object.Instantiate(node, landPos, Quaternion.identity).GetComponent<SpriteRenderer>().color = Color.red;
		LineRenderer component = UnityEngine.Object.Instantiate(line, Vector3.zero, Quaternion.identity).GetComponent<LineRenderer>();
		component.SetPosition(0, jumpPos);
		component.SetPosition(1, landPos);
	}

	private void SaveRun()
	{
		if (!(Vector3.Distance(startRunPos, jumpPos) < 2f))
		{
			UnityEngine.Object.Instantiate(node, startRunPos, Quaternion.identity).GetComponent<SpriteRenderer>().color = Color.blue;
			UnityEngine.Object.Instantiate(node, leaveGroundPos, Quaternion.identity).GetComponent<SpriteRenderer>().color = Color.blue;
			LineRenderer component = UnityEngine.Object.Instantiate(line2, Vector3.zero, Quaternion.identity).GetComponent<LineRenderer>();
			component.SetPosition(0, startRunPos);
			component.SetPosition(1, leaveGroundPos);
		}
	}
}
