using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Photon.Pun;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class ProjectileHit : RayHit
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Comparison<RayHitEffect> _003C_003E9__31_0;

		public static Comparison<RayHitEffect> _003C_003E9__32_0;

		internal int _003CStart_003Eb__31_0(RayHitEffect p1, RayHitEffect p2)
		{
			return p2.priority.CompareTo(p1.priority);
		}

		internal int _003CResortHitEffects_003Eb__32_0(RayHitEffect p1, RayHitEffect p2)
		{
			return p2.priority.CompareTo(p1.priority);
		}
	}

	[HideInInspector]
	public bool canPushBox = true;

	public float force;

	public float damage;

	public float stun;

	public float percentageDamage;

	public float movementSlow;

	public float shake;

	public ObjectsToSpawn[] objectsToSpawn;

	public PlayerSkin team;

	[HideInInspector]
	public Player ownPlayer;

	[HideInInspector]
	public GameObject ownWeapon;

	public AnimationCurve effectOverTimeCurve;

	[HideInInspector]
	public List<RayHitEffect> effects;

	private List<HealthHandler> playersHit = new List<HealthHandler>();

	public Color projectileColor = Color.black;

	private Action hitAction;

	private Action<HitInfo> hitActionWithData;

	[HideInInspector]
	public bool unblockable;

	[HideInInspector]
	public bool fullSelfDamage;

	[FoldoutGroup("Special", 0)]
	public UnityEvent deathEvent;

	[FoldoutGroup("Special", 0)]
	public bool destroyOnBlock;

	[FoldoutGroup("Special", 0)]
	public float holdPlayerFor = 0.5f;

	[FoldoutGroup("Special", 0)]
	public string bulletImmunity = "";

	private SpawnedAttack spawnedAttack;

	[HideInInspector]
	public float sinceReflect = 10f;

	[HideInInspector]
	public float dealDamageMultiplierr = 1f;

	internal bool hasControl;

	[HideInInspector]
	public PhotonView view;

	private MoveTransform move;

	[HideInInspector]
	public bool bulletCanDealDeamage = true;

	[HideInInspector]
	public bool isAllowedToSpawnObjects = true;

	public bool sendCollisions = true;

	public Dictionary<string, Action> customActions = new Dictionary<string, Action>();

	public Dictionary<string, Action<Vector2, Vector2>> customActionsV2V2 = new Dictionary<string, Action<Vector2, Vector2>>();

	private void Start()
	{
		move = GetComponent<MoveTransform>();
		view = GetComponent<PhotonView>();
		effects.AddRange(GetComponentsInChildren<RayHitEffect>());
		effects.Sort(_003C_003Ec._003C_003E9__31_0 ?? (_003C_003Ec._003C_003E9__31_0 = _003C_003Ec._003C_003E9._003CStart_003Eb__31_0));
		spawnedAttack = GetComponent<SpawnedAttack>();
		if ((bool)spawnedAttack && !ownPlayer)
		{
			ownPlayer = spawnedAttack.spawner;
		}
		if ((bool)ownPlayer && !fullSelfDamage)
		{
			StartCoroutine(HoldPlayer(ownPlayer.GetComponent<HealthHandler>()));
		}
		damage *= base.transform.localScale.x;
		force *= Mathf.Pow(damage / 55f, 2f);
	}

	public void ResortHitEffects()
	{
		effects.Sort(_003C_003Ec._003C_003E9__32_0 ?? (_003C_003Ec._003C_003E9__32_0 = _003C_003Ec._003C_003E9._003CResortHitEffects_003Eb__32_0));
	}

	private IEnumerator HoldPlayer(HealthHandler player)
	{
		if ((bool)player)
		{
			playersHit.Add(player);
		}
		yield return new WaitForSeconds(holdPlayerFor);
		if (playersHit.Contains(player))
		{
			playersHit.Remove(player);
		}
	}

	private void Update()
	{
		sinceReflect += TimeHandler.deltaTime;
	}

	public void AddPlayerToHeld(HealthHandler health)
	{
		StartCoroutine(HoldPlayer(health));
	}

	public void RemoveOwnPlayerFromPlayersHit()
	{
		if ((bool)ownPlayer && playersHit.Contains(ownPlayer.GetComponent<HealthHandler>()))
		{
			playersHit.Remove(ownPlayer.GetComponent<HealthHandler>());
		}
	}

	public override void Hit(HitInfo hit, bool forceCall = false)
	{
		int num = -1;
		if ((bool)hit.transform)
		{
			PhotonView component = hit.transform.root.GetComponent<PhotonView>();
			if ((bool)component)
			{
				num = component.ViewID;
			}
		}
		int num2 = -1;
		if (num == -1)
		{
			Collider2D[] componentsInChildren = MapManager.instance.currentMap.Map.GetComponentsInChildren<Collider2D>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if (componentsInChildren[i] == hit.collider)
				{
					num2 = i;
				}
			}
		}
		HealthHandler healthHandler = null;
		if ((bool)hit.transform)
		{
			healthHandler = hit.transform.GetComponent<HealthHandler>();
		}
		bool flag = false;
		if ((bool)healthHandler)
		{
			if (playersHit.Contains(healthHandler))
			{
				return;
			}
			if (view.IsMine && healthHandler.GetComponent<Block>().IsBlocking())
			{
				flag = true;
			}
			StartCoroutine(HoldPlayer(healthHandler));
		}
		if (view.IsMine || forceCall)
		{
			if (sendCollisions)
			{
				view.RPC("RPCA_DoHit", RpcTarget.All, hit.point, hit.normal, (Vector2)move.velocity, num, num2, flag);
			}
			else
			{
				RPCA_DoHit(hit.point, hit.normal, move.velocity, num, num2, flag);
			}
		}
	}

	[PunRPC]
	public void RPCA_DoHit(Vector2 hitPoint, Vector2 hitNormal, Vector2 vel, int viewID = -1, int colliderID = -1, bool wasBlocked = false)
	{
		HitInfo hitInfo = new HitInfo();
		if ((bool)move)
		{
			move.velocity = vel;
		}
		hitInfo.point = hitPoint;
		hitInfo.normal = hitNormal;
		hitInfo.collider = null;
		if (viewID != -1)
		{
			PhotonView photonView = PhotonNetwork.GetPhotonView(viewID);
			hitInfo.collider = photonView.GetComponentInChildren<Collider2D>();
			hitInfo.transform = photonView.transform;
		}
		else if (colliderID != -1)
		{
			hitInfo.collider = MapManager.instance.currentMap.Map.GetComponentsInChildren<Collider2D>()[colliderID];
			hitInfo.transform = hitInfo.collider.transform;
		}
		HealthHandler healthHandler = null;
		if ((bool)hitInfo.transform)
		{
			healthHandler = hitInfo.transform.GetComponent<HealthHandler>();
		}
		if (isAllowedToSpawnObjects)
		{
			base.transform.position = hitInfo.point;
		}
		if ((bool)hitInfo.collider)
		{
			ProjectileHitSurface component = hitInfo.collider.GetComponent<ProjectileHitSurface>();
			if ((bool)component && component.HitSurface(hitInfo, base.gameObject) == ProjectileHitSurface.HasToStop.HasToStop)
			{
				return;
			}
		}
		if ((bool)healthHandler)
		{
			Block component2 = healthHandler.GetComponent<Block>();
			if (wasBlocked)
			{
				component2.DoBlock(base.gameObject, base.transform.forward, hitInfo.point);
				if (destroyOnBlock)
				{
					DestroyMe();
				}
				sinceReflect = 0f;
				return;
			}
			CharacterStatModifiers component3 = healthHandler.GetComponent<CharacterStatModifiers>();
			if (movementSlow != 0f && !wasBlocked)
			{
				component3.RPCA_AddSlow(movementSlow);
			}
		}
		float num = 1f;
		PlayerVelocity playerVelocity = null;
		if ((bool)hitInfo.transform)
		{
			playerVelocity = hitInfo.transform.GetComponentInParent<PlayerVelocity>();
		}
		if ((bool)hitInfo.collider)
		{
			Damagable componentInParent = hitInfo.collider.GetComponentInParent<Damagable>();
			if ((bool)componentInParent)
			{
				if ((bool)healthHandler && percentageDamage != 0f)
				{
					damage += healthHandler.GetComponent<CharacterData>().maxHealth * percentageDamage;
				}
				if (hasControl)
				{
					if (bulletImmunity != "" && (bool)healthHandler)
					{
						healthHandler.GetComponent<PlayerImmunity>().IsImune(0.1f, (bulletCanDealDeamage ? damage : 1f) * dealDamageMultiplierr, bulletImmunity);
					}
					if ((bool)componentInParent.GetComponent<DamagableEvent>())
					{
						componentInParent.CallTakeDamage(base.transform.forward * damage * dealDamageMultiplierr, hitInfo.point, ownWeapon, ownPlayer);
					}
					else
					{
						componentInParent.CallTakeDamage(base.transform.forward * (bulletCanDealDeamage ? damage : 1f) * dealDamageMultiplierr, hitInfo.point, ownWeapon, ownPlayer);
					}
				}
			}
		}
		if ((bool)playerVelocity)
		{
			float num2 = 1f;
			float num3 = Mathf.Clamp(playerVelocity.mass / 100f * num2, 0f, 1f) * num2;
			float num4 = 1f;
			playerVelocity.AddForce(-playerVelocity.velocity * 0.1f * playerVelocity.mass, ForceMode2D.Impulse);
			if ((bool)healthHandler)
			{
				num *= 3f;
				if (hasControl)
				{
					healthHandler.CallTakeForce(base.transform.forward * num4 * num3 * force);
				}
			}
		}
		if (isAllowedToSpawnObjects && !wasBlocked)
		{
			GamefeelManager.GameFeel(base.transform.forward * num * shake);
			DynamicParticles.instance.PlayBulletHit(damage, base.transform, hitInfo, projectileColor);
			for (int i = 0; i < objectsToSpawn.Length; i++)
			{
				ObjectsToSpawn.SpawnObject(base.transform, hitInfo, objectsToSpawn[i], healthHandler, team, damage, spawnedAttack, wasBlocked);
			}
			base.transform.position = hitInfo.point + hitInfo.normal * 0.01f;
		}
		if ((bool)hitInfo.transform)
		{
			NetworkPhysicsObject component4 = hitInfo.transform.GetComponent<NetworkPhysicsObject>();
			if ((bool)component4 && canPushBox)
			{
				component4.BulletPush(base.transform.forward * (force * 0.5f + damage * 100f), hitInfo.transform.InverseTransformPoint(hitInfo.point), spawnedAttack.spawner.data);
			}
		}
		bool flag = false;
		if (effects != null && effects.Count != 0)
		{
			for (int j = 0; j < effects.Count; j++)
			{
				HasToReturn num5 = effects[j].DoHitEffect(hitInfo);
				if (num5 == HasToReturn.hasToReturn)
				{
					flag = true;
				}
				if (num5 == HasToReturn.hasToReturnNow)
				{
					return;
				}
			}
		}
		if (!flag)
		{
			if (hitAction != null)
			{
				hitAction();
			}
			if (hitActionWithData != null)
			{
				hitActionWithData(hitInfo);
			}
			deathEvent.Invoke();
			DestroyMe();
		}
	}

	private void DestroyMe()
	{
		if ((bool)view)
		{
			if (view.IsMine)
			{
				PhotonNetwork.Destroy(base.gameObject);
			}
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	[PunRPC]
	public void RPCA_CallCustomAction(string actionKey)
	{
		customActions[actionKey]();
	}

	[PunRPC]
	public void RPCA_CallCustomActionV2V2(string actionKey, Vector2 v1, Vector2 v2)
	{
		customActionsV2V2[actionKey](v1, v2);
	}

	public void AddHitAction(Action action)
	{
		hitAction = (Action)Delegate.Combine(hitAction, action);
	}

	public void AddHitActionWithData(Action<HitInfo> action)
	{
		hitActionWithData = (Action<HitInfo>)Delegate.Combine(hitActionWithData, action);
	}
}
