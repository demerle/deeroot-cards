using System.Collections;
using UnityEngine;

public class GeneralInput : MonoBehaviour
{
	public enum InputType
	{
		Controller,
		Keyboard,
		Either
	}

	public enum StickDirection
	{
		Left,
		Right,
		None
	}

	public bool controlledElseWhere;

	public bool stunnedInput;

	public bool silencedInput;

	public KeyCode left;

	public KeyCode right;

	public KeyCode up;

	public KeyCode down;

	public KeyCode jump;

	public KeyCode shoot;

	public KeyCode shield;

	public Vector3 direction;

	public Vector3 latestPressedDirection;

	public Vector3 aimDirection;

	public Vector3 lastAimDirection;

	public bool jumpWasPressed;

	public bool jumpIsPressed;

	public bool shootWasPressed;

	public bool shootIsPressed;

	public bool shootWasReleased;

	public bool shieldWasPressed;

	public bool acceptWasPressed;

	public InputType inputType;

	private StickDirection lastStickDirection;

	public StickDirection stickPressDir = StickDirection.None;

	private CharacterData data;

	private int snapNumber = 16;

	private void Start()
	{
		data = GetComponent<CharacterData>();
	}

	public void ResetInput()
	{
		direction = Vector3.zero;
		aimDirection = Vector3.zero;
		jumpIsPressed = false;
		jumpWasPressed = false;
		shootIsPressed = false;
		shootWasPressed = false;
		shootWasReleased = false;
		shieldWasPressed = false;
		acceptWasPressed = false;
	}

	private void Update()
	{
		if (controlledElseWhere)
		{
			return;
		}
		ResetInput();
		DoUIInput();
		if (GameManager.lockInput || stunnedInput || !data.isPlaying || data.playerActions == null)
		{
			return;
		}
		direction += (Vector3)data.playerActions.Move;
		direction = MakeEightDirections(direction);
		if (direction != Vector3.zero)
		{
			latestPressedDirection = direction;
		}
		if (data.playerActions.Device == null)
		{
			aimDirection = MainCam.instance.cam.ScreenToWorldPoint(Input.mousePosition) - base.transform.position;
			aimDirection.z = 0f;
			aimDirection.Normalize();
			if (Optionshandler.lockMouse)
			{
				aimDirection = MakeEightDirections(aimDirection);
			}
		}
		else
		{
			aimDirection.x += data.playerActions.Aim.X;
			aimDirection.y += data.playerActions.Aim.Y;
			if (Optionshandler.lockStick)
			{
				aimDirection = MakeEightDirections(aimDirection);
			}
		}
		if (aimDirection != Vector3.zero)
		{
			aimDirection += Vector3.up * 0.13f / Mathf.Clamp(data.weaponHandler.gun.projectileSpeed, 1f, 100f);
		}
		if (aimDirection != Vector3.zero)
		{
			lastAimDirection = aimDirection;
		}
		if (data.playerActions.Jump.IsPressed)
		{
			jumpIsPressed = true;
		}
		if (data.playerActions.Jump.WasPressed)
		{
			jumpWasPressed = true;
		}
		if (!silencedInput)
		{
			if (data.playerActions.Fire.IsPressed)
			{
				shootIsPressed = true;
			}
			if (data.playerActions.Fire.WasPressed)
			{
				shootWasPressed = true;
			}
			if (data.playerActions.Fire.WasReleased)
			{
				shootWasReleased = true;
			}
			if (data.playerActions.Block.WasPressed)
			{
				shieldWasPressed = true;
			}
		}
	}

	private void DoUIInput()
	{
		StickDirection stickDirection = StickDirection.None;
		if (data.playerActions.Move.X > 0.7f)
		{
			stickDirection = StickDirection.Right;
		}
		if (data.playerActions.Move.X < -0.7f)
		{
			stickDirection = StickDirection.Left;
		}
		if (stickDirection != lastStickDirection)
		{
			stickPressDir = stickDirection;
			lastStickDirection = stickDirection;
		}
		else
		{
			stickPressDir = StickDirection.None;
		}
		if (data.playerActions.Jump.WasPressed)
		{
			acceptWasPressed = true;
		}
	}

	public void DoStun(float stun)
	{
		if (base.gameObject.activeSelf)
		{
			StartCoroutine(Stun(stun));
		}
	}

	private IEnumerator Stun(float stun)
	{
		base.enabled = false;
		ResetInput();
		yield return new WaitForSeconds(stun);
		base.enabled = true;
	}

	public void SetState(Vector3 pos, bool isGrounded)
	{
		base.transform.position = pos;
	}

	public void Move(bool right, bool left, bool up, bool down, bool jump)
	{
		ResetInput();
		if (left)
		{
			direction += Vector3.left;
		}
		if (right)
		{
			direction += Vector3.right;
		}
		if (up)
		{
			direction += Vector3.up;
		}
		if (down)
		{
			direction += Vector3.down;
		}
		if (jump)
		{
			jumpWasPressed = true;
		}
		Debug.Log("Move: Right: " + right + " Left: " + left + " Jump: " + jump);
		if (Input.GetKey(shoot))
		{
			shootIsPressed = true;
		}
		if (Input.GetKeyDown(shoot))
		{
			shootWasPressed = true;
		}
		if (Input.GetKeyUp(shoot))
		{
			shootWasReleased = true;
		}
	}

	private Vector3 MakeEightDirections(Vector3 dir)
	{
		_ = Vector3.zero;
		float num = 360f / (float)snapNumber;
		Vector3 vector = Vector3.up;
		Vector3 vector2 = vector;
		float num2 = 999f;
		Vector3 result = dir;
		for (int i = 0; i < snapNumber; i++)
		{
			vector2 = Quaternion.Euler(Vector3.forward * num * i) * vector;
			float num3 = Vector3.Angle(dir, vector2);
			if (num3 < num2)
			{
				num2 = num3;
				result = vector2;
			}
		}
		if (dir == Vector3.zero)
		{
			return Vector3.zero;
		}
		dir = dir.normalized;
		return result;
	}
}
