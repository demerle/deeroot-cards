using Photon.Pun;
using Photon.Pun.Simple;
using UnityEngine;

public class SuperBasicController : NetComponent
{
	public bool is2D;

	[Range(0f, 300f)]
	public float turnSpeed = 150f;

	[Range(0f, 4f)]
	public float moveSpeed = 4f;

	public bool autoMove = true;

	private Vector3 targRotDelta;

	private Vector3 targPosDelta;

	private float appliedDeltaT;

	private Animator animator;

	private SyncAnimator syncAnimator;

	private SyncTransform syncTransform;

	private SyncCannon syncLauncher;

	private SyncContactScan syncHitscan;

	private bool triggerJump;

	private bool triggerFade;

	private bool triggerTurnLeft;

	private bool triggerUpperBodyRun;

	private bool triggerUpperBodyIdle;

	private bool triggerTeleport;

	private bool freakingOut;

	private bool triggerHitscan;

	private bool triggerProjectile;

	private bool triggerBlend;

	public override void OnAwake()
	{
		base.OnAwake();
		animator = base.transform.GetNestedComponentInChildren<Animator, NetObject>(includeInactive: true);
		syncAnimator = base.transform.GetNestedComponentInChildren<SyncAnimator, NetObject>(includeInactive: true);
		syncTransform = GetComponent<SyncTransform>();
		syncLauncher = base.transform.GetNestedComponentInChildren<SyncCannon, NetObject>(includeInactive: true);
		syncHitscan = base.transform.GetNestedComponentInChildren<SyncContactScan, NetObject>(includeInactive: true);
	}

	private void Update()
	{
		if (!base.IsMine)
		{
			return;
		}
		float t = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
		Interpolate(t);
		if (Input.GetKeyDown(KeyCode.Space))
		{
			triggerJump = true;
		}
		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			triggerFade = true;
		}
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			triggerTurnLeft = true;
		}
		if (Input.GetKeyDown(KeyCode.Alpha4))
		{
			if (freakingOut)
			{
				triggerUpperBodyIdle = true;
			}
			else
			{
				triggerUpperBodyRun = true;
			}
			freakingOut = !freakingOut;
		}
		if (Input.GetKeyDown(KeyCode.F))
		{
			triggerProjectile = true;
		}
		if (Input.GetKeyDown(KeyCode.R))
		{
			triggerHitscan = true;
		}
		if (Input.GetKeyDown(KeyCode.T))
		{
			triggerTeleport = true;
		}
		if (Input.GetKeyDown(KeyCode.B))
		{
			triggerBlend = true;
		}
	}

	private void FixedUpdate()
	{
		if (!base.IsMine)
		{
			return;
		}
		Vector3 move = new Vector3(0f, 0f, 0f);
		Vector3 turn = new Vector3(0f, 0f, 0f);
		if ((bool)animator && animator.isActiveAndEnabled)
		{
			if (Input.GetKey(KeyCode.W))
			{
				animator.SetBool("walking", value: true);
				animator.SetFloat("speed", 1f);
			}
			else if (Input.GetKey(KeyCode.S))
			{
				animator.SetBool("walking", value: true);
				animator.SetFloat("speed", -0.5f);
			}
			else
			{
				animator.SetBool("walking", value: false);
				animator.SetFloat("speed", 0f);
			}
			if (triggerJump)
			{
				if ((bool)syncAnimator)
				{
					syncAnimator.SetTrigger("jump");
				}
				triggerJump = false;
			}
			else if (triggerTurnLeft)
			{
				if ((bool)syncAnimator)
				{
					syncAnimator.SetTrigger("turnLeft");
				}
				triggerTurnLeft = false;
			}
			if (triggerFade)
			{
				if ((bool)syncAnimator)
				{
					syncAnimator.CrossFadeInFixedTime("Jump", 0.25f);
				}
				triggerFade = false;
			}
			if (triggerBlend)
			{
				animator.SetFloat("blender", Mathf.Abs(Mathf.Sin(Time.time)));
			}
			else
			{
				animator.SetFloat("blender", -1f);
			}
			if (triggerUpperBodyRun)
			{
				if ((bool)syncAnimator)
				{
					syncAnimator.SetTrigger("upperBodyRun");
				}
				triggerUpperBodyRun = false;
			}
			else if (triggerUpperBodyIdle)
			{
				if ((bool)syncAnimator)
				{
					syncAnimator.SetTrigger("upperBodyIdle");
				}
				triggerUpperBodyIdle = false;
			}
		}
		if (!animator || !animator.applyRootMotion)
		{
			if (Input.GetKey(KeyCode.W))
			{
				move += Vector3.forward;
			}
			else if (Input.GetKey(KeyCode.S))
			{
				move -= Vector3.forward;
			}
		}
		if (Input.GetKey(KeyCode.A))
		{
			move -= Vector3.right;
		}
		if (Input.GetKey(KeyCode.D))
		{
			move += Vector3.right;
		}
		if (Input.GetKey(KeyCode.E))
		{
			turn += Vector3.up;
		}
		if (Input.GetKey(KeyCode.Q))
		{
			turn -= Vector3.up;
		}
		if (Input.touchCount > 0)
		{
			Touch touch = Input.GetTouch(0);
			Vector2 vector = new Vector2(touch.rawPosition.x / (float)Screen.width, touch.rawPosition.y / (float)Screen.height);
			if (vector.y > 0.66f)
			{
				if (vector.x > 0.66f)
				{
					triggerHitscan = true;
				}
				else if (vector.x < 0.33f)
				{
					triggerJump = true;
				}
			}
			else if (vector.y < 0.33f)
			{
				if (vector.x > 0.66f)
				{
					move += Vector3.right;
				}
				else if (vector.x < 0.33f)
				{
					move -= Vector3.right;
				}
				else if ((bool)animator)
				{
					animator.SetBool("walking", value: true);
					animator.SetFloat("speed", -0.5f);
				}
			}
			else if (vector.x > 0.66f)
			{
				turn += Vector3.up;
			}
			else if (vector.x < 0.33f)
			{
				turn -= Vector3.up;
			}
			else if ((bool)animator)
			{
				animator.SetBool("walking", value: true);
				animator.SetFloat("speed", 1f);
			}
		}
		if (autoMove && !Application.isFocused)
		{
			turn += new Vector3(0f, Mathf.Sin(Time.time * 0.5f), 0f);
			if ((bool)animator)
			{
				animator.SetBool("walking", value: true);
				animator.SetFloat("speed", Mathf.Sin(Time.time) * 0.5f);
			}
		}
		Interpolate(1f);
		Move(move, turn);
		appliedDeltaT = 0f;
		if (triggerHitscan)
		{
			if ((bool)syncHitscan)
			{
				syncHitscan.QueueTrigger();
			}
			triggerHitscan = false;
		}
		if (triggerProjectile)
		{
			if ((bool)syncLauncher)
			{
				syncLauncher.QueueTrigger();
			}
			triggerProjectile = false;
		}
		if (triggerTeleport)
		{
			if ((bool)syncTransform)
			{
				syncTransform.FlagTeleport();
				base.transform.localPosition = default(Vector3);
				base.transform.localRotation = default(Quaternion);
			}
			triggerTeleport = false;
		}
	}

	private void OnAnimatorMove()
	{
		if (base.IsMine)
		{
			animator.ApplyBuiltinRootMotion();
			base.transform.rotation = animator.rootRotation;
			base.transform.position = animator.rootPosition;
		}
	}

	private void Move(Vector3 move, Vector3 turn)
	{
		if (is2D)
		{
			move = new Vector3(move.x, move.z, 0f);
			turn = new Vector3(0f, 0f, turn.y);
		}
		targRotDelta = turn * turnSpeed * Time.fixedDeltaTime;
		targPosDelta = move * moveSpeed * Time.fixedDeltaTime;
	}

	private void Interpolate(float t)
	{
		t -= appliedDeltaT;
		appliedDeltaT += t;
		base.transform.rotation = base.transform.rotation * Quaternion.Euler(targRotDelta * t);
		base.transform.position += base.transform.rotation * (targPosDelta * t);
	}
}
