using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class CharacterData : MonoBehaviour
{
	public Vector3 aimDirection;

	public List<CardInfo> currentCards;

	public float sinceGroundedMultiplierWhenWallGrab = 0.2f;

	public PlayerActions playerActions;

	public ParticleSystem[] landParts;

	public int jumps = 1;

	public int currentJumps = 1;

	public bool isPlaying;

	public bool dead;

	public bool isStunned;

	public bool isSilenced;

	public float stunTime;

	public float silenceTime;

	public float health = 100f;

	public float maxHealth = 100f;

	public AnimationCurve slamCurve;

	public Vector3 wallPos;

	public Vector2 wallNormal;

	public Vector3 groundPos;

	public Transform hand;

	public float sinceWallGrab = float.PositiveInfinity;

	public bool isWallGrab;

	public float wallDistance = 1f;

	private bool wasWallGrabLastFrame;

	public float sinceGrounded;

	public bool isGrounded = true;

	private bool wasGroundedLastFrame = true;

	public Player player;

	public float sinceJump = 1f;

	public PlayerVelocity playerVel;

	public HealthHandler healthHandler;

	public GeneralInput input;

	public PlayerMovement movement;

	public PlayerJump jump;

	public Block block;

	public CharacterStatModifiers stats;

	public WeaponHandler weaponHandler;

	public StunHandler stunHandler;

	public SilenceHandler silenceHandler;

	public Player lastSourceOfDamage;

	public Player master;

	public Player lastDamagedPlayer;

	public Collider2D mainCol;

	public PlayerSounds playerSounds;

	private Transform wobblePos;

	private LayerMask groundMask;

	public PhotonView view;

	private CrownPos crownPos;

	public Rigidbody2D standOnRig;

	public Action<float, Vector3, Vector3, Transform> TouchGroundAction;

	public Action<float, Vector3, Vector3> TouchWallAction;

	public float HealthPercentage
	{
		get
		{
			return health / maxHealth;
		}
		internal set
		{
		}
	}

	private void Awake()
	{
		crownPos = GetComponentInChildren<CrownPos>();
		view = GetComponent<PhotonView>();
		mainCol = GetComponent<Collider2D>();
		wobblePos = GetComponentInChildren<PlayerWobblePosition>().transform;
		stats = GetComponent<CharacterStatModifiers>();
		player = GetComponent<Player>();
		weaponHandler = GetComponent<WeaponHandler>();
		block = GetComponent<Block>();
		input = GetComponent<GeneralInput>();
		movement = GetComponent<PlayerMovement>();
		jump = GetComponent<PlayerJump>();
		stunHandler = GetComponent<StunHandler>();
		silenceHandler = GetComponent<SilenceHandler>();
		hand = GetComponentInChildren<HandPos>().transform;
		playerVel = GetComponent<PlayerVelocity>();
		healthHandler = GetComponent<HealthHandler>();
		playerSounds = GetComponent<PlayerSounds>();
	}

	internal Vector3 GetCrownPos()
	{
		if ((bool)crownPos)
		{
			return crownPos.transform.position + Vector3.up * crownPos.GetOffset();
		}
		Debug.LogError("NO CROWN POS!?");
		return Vector3.up * 1000f;
	}

	private void Start()
	{
		groundMask = LayerMask.GetMask("Default");
		if (!view.IsMine)
		{
			PlayerManager.RegisterPlayer(player);
		}
	}

	private void Update()
	{
		if (!playerVel.simulated)
		{
			sinceGrounded = 0f;
		}
		sinceJump += TimeHandler.deltaTime;
		Wall();
	}

	private void FixedUpdate()
	{
		Ground();
	}

	private void Ground()
	{
		if (!isPlaying)
		{
			return;
		}
		if (!isGrounded)
		{
			sinceGrounded += TimeHandler.fixedDeltaTime * ((isWallGrab && wallDistance < 0.7f) ? sinceGroundedMultiplierWhenWallGrab : 1f);
			if (sinceGrounded < 0f)
			{
				sinceGrounded = Mathf.Lerp(sinceGrounded, 0f, TimeHandler.fixedDeltaTime * 15f);
			}
		}
		if (!wasGroundedLastFrame)
		{
			isGrounded = false;
		}
		wasGroundedLastFrame = false;
	}

	private void Wall()
	{
		if (!isWallGrab)
		{
			sinceWallGrab += TimeHandler.deltaTime;
		}
		if (!wasWallGrabLastFrame)
		{
			isWallGrab = false;
		}
		wasWallGrabLastFrame = false;
	}

	public void TouchGround(Vector3 pos, Vector3 groundNormal, Rigidbody2D groundRig, Transform groundTransform = null)
	{
		if (sinceJump > 0.2f)
		{
			currentJumps = jumps;
		}
		if (TouchGroundAction != null)
		{
			TouchGroundAction(sinceGrounded, pos, groundNormal, groundTransform);
		}
		if (groundRig == null)
		{
			standOnRig = null;
		}
		else if (!groundRig.GetComponent<NetworkPhysicsObject>())
		{
			standOnRig = groundRig;
		}
		if (playerVel.velocity.y < -20f && !isGrounded)
		{
			for (int i = 0; i < landParts.Length; i++)
			{
				landParts[i].transform.localScale = Vector3.one * Mathf.Clamp((0f - playerVel.velocity.y) / 40f, 0.5f, 1f) * 0.5f;
				landParts[i].transform.position = new Vector3(base.transform.position.x + playerVel.velocity.x * 0.03f, pos.y, 5f);
				landParts[i].transform.rotation = Quaternion.LookRotation(groundNormal);
				landParts[i].Play();
			}
			GamefeelManager.instance.AddGameFeel(Vector2.down * Mathf.Clamp((sinceGrounded - 0.5f) * 1f, 0f, 4f));
		}
		groundPos = pos;
		wasGroundedLastFrame = true;
		isGrounded = true;
		sinceGrounded = 0f;
	}

	public void TouchWall(Vector2 normal, Vector3 pos)
	{
		if (isGrounded)
		{
			return;
		}
		wallNormal = normal;
		wallPos = pos;
		groundPos = pos;
		wallDistance = Vector2.Distance(base.transform.position, pos);
		if (!(sinceJump < 0.15f))
		{
			currentJumps = jumps;
			if (TouchWallAction != null)
			{
				TouchWallAction(sinceWallGrab, pos, normal);
			}
			_ = sinceWallGrab;
			_ = 0.15f;
			sinceWallGrab = 0f;
			wasWallGrabLastFrame = true;
			isWallGrab = true;
		}
	}

	public bool ThereIsGroundBelow(Vector3 pos, float range = 5f)
	{
		RaycastHit2D raycastHit2D = Physics2D.Raycast(pos, Vector2.down, range, groundMask);
		if ((bool)raycastHit2D.transform && raycastHit2D.distance > 0.1f)
		{
			return true;
		}
		return false;
	}

	public void SetAI(Player aiMaster = null)
	{
		master = aiMaster;
		input.controlledElseWhere = true;
		GetComponent<PlayerAPI>().enabled = true;
	}

	public void SetWobbleObjectChild(Transform obj)
	{
		obj.transform.SetParent(wobblePos, worldPositionStays: true);
	}
}
