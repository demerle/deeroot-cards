using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Sirenix.OdinInspector;
using SoundImplementation;
using UnityEngine;

public class Gun : Weapon
{
	[Header("Sound Player Settings")]
	public SoundGun soundGun = new SoundGun();

	[Header("Sound Card Settings")]
	public SoundShotModifier soundShotModifier;

	public SoundImpactModifier soundImpactModifier;

	public bool soundDisableRayHitBulletSound;

	[Header("Settings")]
	public float recoil;

	public float bodyRecoil;

	public float shake;

	public bool forceSpecificShake;

	public ProjectilesToSpawn[] projectiles;

	private Rigidbody2D rig;

	public Transform shootPosition;

	[HideInInspector]
	public Player player;

	[HideInInspector]
	public bool isReloading;

	[Header("Multiply")]
	[FoldoutGroup("Stats", 0)]
	public float damage = 1f;

	[FoldoutGroup("Stats", 0)]
	public float reloadTime = 1f;

	[FoldoutGroup("Stats", 0)]
	public float reloadTimeAdd;

	[FoldoutGroup("Stats", 0)]
	public float recoilMuiltiplier = 1f;

	[FoldoutGroup("Stats", 0)]
	public float knockback = 1f;

	[FoldoutGroup("Stats", 0)]
	public float attackSpeed = 1f;

	[FoldoutGroup("Stats", 0)]
	public float projectileSpeed = 1f;

	[FoldoutGroup("Stats", 0)]
	public float projectielSimulatonSpeed = 1f;

	[FoldoutGroup("Stats", 0)]
	public float gravity = 1f;

	[FoldoutGroup("Stats", 0)]
	public float damageAfterDistanceMultiplier = 1f;

	[FoldoutGroup("Stats", 0)]
	public float bulletDamageMultiplier = 1f;

	[FoldoutGroup("Stats", 0)]
	public float multiplySpread = 1f;

	[FoldoutGroup("Stats", 0)]
	public float shakeM = 1f;

	[Header("Add")]
	[FoldoutGroup("Stats", 0)]
	public int ammo;

	[FoldoutGroup("Stats", 0)]
	public float ammoReg;

	[FoldoutGroup("Stats", 0)]
	public float size;

	[FoldoutGroup("Stats", 0)]
	public float overheatMultiplier;

	[FoldoutGroup("Stats", 0)]
	public float timeToReachFullMovementMultiplier;

	[FoldoutGroup("Stats", 0)]
	public int numberOfProjectiles;

	[FoldoutGroup("Stats", 0)]
	public int bursts;

	[FoldoutGroup("Stats", 0)]
	public int reflects;

	[FoldoutGroup("Stats", 0)]
	public int smartBounce;

	[FoldoutGroup("Stats", 0)]
	public int bulletPortal;

	[FoldoutGroup("Stats", 0)]
	public int randomBounces;

	[FoldoutGroup("Stats", 0)]
	public float timeBetweenBullets;

	[FoldoutGroup("Stats", 0)]
	public float projectileSize;

	[FoldoutGroup("Stats", 0)]
	public float speedMOnBounce = 1f;

	[FoldoutGroup("Stats", 0)]
	public float dmgMOnBounce = 1f;

	[FoldoutGroup("Stats", 0)]
	public float drag;

	[FoldoutGroup("Stats", 0)]
	public float dragMinSpeed = 1f;

	[FoldoutGroup("Stats", 0)]
	public float spread;

	[FoldoutGroup("Stats", 0)]
	public float evenSpread;

	[FoldoutGroup("Stats", 0)]
	public float percentageDamage;

	[FoldoutGroup("Stats", 0)]
	public float cos;

	[FoldoutGroup("Stats", 0)]
	public float slow;

	[FoldoutGroup("Stats", 0)]
	[Header("Charge Multiply")]
	public float chargeDamageMultiplier = 1f;

	[FoldoutGroup("Stats", 0)]
	[Header("(1 + Charge * x) Multiply")]
	public float chargeSpreadTo;

	[FoldoutGroup("Stats", 0)]
	public float chargeEvenSpreadTo;

	[FoldoutGroup("Stats", 0)]
	public float chargeSpeedTo;

	[FoldoutGroup("Stats", 0)]
	public float chargeRecoilTo;

	[FoldoutGroup("Stats", 0)]
	[Header("(1 + Charge * x) Add")]
	public float chargeNumberOfProjectilesTo;

	[FoldoutGroup("Stats", 0)]
	[Header("Special")]
	public float destroyBulletAfter;

	[FoldoutGroup("Stats", 0)]
	public float forceSpecificAttackSpeed;

	[FoldoutGroup("Stats", 0)]
	public bool lockGunToDefault;

	[FoldoutGroup("Stats", 0)]
	public bool unblockable;

	[FoldoutGroup("Stats", 0)]
	public bool ignoreWalls;

	[HideInInspector]
	public float currentCharge;

	public bool useCharge;

	public bool dontAllowAutoFire;

	public ObjectsToSpawn[] objectsToSpawn;

	public Color projectileColor = Color.black;

	public bool waveMovement;

	public bool teleport;

	public bool spawnSkelletonSquare;

	public float explodeNearEnemyRange;

	public float explodeNearEnemyDamage;

	public float hitMovementMultiplier = 1f;

	private Action attackAction;

	[HideInInspector]
	public bool isProjectileGun;

	[HideInInspector]
	public float defaultCooldown = 1f;

	[HideInInspector]
	public int attackID = -1;

	public float attackSpeedMultiplier = 1f;

	private int gunID = -1;

	private GunAmmo gunAmmo;

	private Vector3 spawnPos;

	public Action<GameObject> ShootPojectileAction;

	private float spreadOfLastBullet;

	private SpawnedAttack spawnedAttack;

	[HideInInspector]
	internal Vector3 forceShootDir;

	private float usedCooldown
	{
		get
		{
			if (!lockGunToDefault)
			{
				return attackSpeed;
			}
			return defaultCooldown;
		}
	}

	internal float GetRangeCompensation(float distance)
	{
		return Mathf.Pow(distance, 2f) * 0.015f / projectileSpeed;
	}

	internal void ResetStats()
	{
		isReloading = false;
		damage = 1f;
		reloadTime = 1f;
		reloadTimeAdd = 0f;
		recoilMuiltiplier = 1f;
		gunAmmo.reloadTimeMultiplier = 1f;
		gunAmmo.reloadTimeAdd = 0f;
		knockback = 1f;
		attackSpeed = 0.3f;
		projectileSpeed = 1f;
		projectielSimulatonSpeed = 1f;
		gravity = 1f;
		damageAfterDistanceMultiplier = 1f;
		bulletDamageMultiplier = 1f;
		multiplySpread = 1f;
		shakeM = 1f;
		ammo = 0;
		ammoReg = 0f;
		size = 0f;
		overheatMultiplier = 0f;
		timeToReachFullMovementMultiplier = 0f;
		numberOfProjectiles = 1;
		bursts = 0;
		reflects = 0;
		smartBounce = 0;
		bulletPortal = 0;
		randomBounces = 0;
		timeBetweenBullets = 0f;
		projectileSize = 0f;
		speedMOnBounce = 1f;
		dmgMOnBounce = 1f;
		drag = 0f;
		dragMinSpeed = 1f;
		spread = 0f;
		evenSpread = 0f;
		percentageDamage = 0f;
		cos = 0f;
		slow = 0f;
		chargeNumberOfProjectilesTo = 0f;
		destroyBulletAfter = 0f;
		forceSpecificAttackSpeed = 0f;
		lockGunToDefault = false;
		unblockable = false;
		ignoreWalls = false;
		currentCharge = 0f;
		useCharge = false;
		waveMovement = false;
		teleport = false;
		spawnSkelletonSquare = false;
		explodeNearEnemyRange = 0f;
		explodeNearEnemyDamage = 0f;
		hitMovementMultiplier = 1f;
		isProjectileGun = false;
		defaultCooldown = 1f;
		attackSpeedMultiplier = 1f;
		objectsToSpawn = new ObjectsToSpawn[0];
		GetComponentInChildren<GunAmmo>().maxAmmo = 3;
		GetComponentInChildren<GunAmmo>().ReDrawTotalBullets();
		projectileColor = Color.black;
	}

	private void Start()
	{
		gunAmmo = GetComponentInChildren<GunAmmo>();
		Gun[] componentsInChildren = base.transform.root.GetComponentsInChildren<Gun>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i] == this)
			{
				gunID = i;
			}
		}
		if (!player)
		{
			player = GetComponentInParent<Player>();
		}
		if (!player)
		{
			ProjectileHit componentInParent = GetComponentInParent<ProjectileHit>();
			if ((bool)componentInParent)
			{
				player = componentInParent.ownPlayer;
			}
		}
		if (!player)
		{
			SpawnedAttack component = base.transform.root.GetComponent<SpawnedAttack>();
			if ((bool)component)
			{
				player = component.spawner;
			}
		}
		holdable = GetComponent<Holdable>();
		defaultCooldown = usedCooldown;
		ShootPos componentInChildren = GetComponentInChildren<ShootPos>();
		if ((bool)componentInChildren)
		{
			shootPosition = componentInChildren.transform;
		}
		else
		{
			shootPosition = base.transform;
		}
		rig = GetComponent<Rigidbody2D>();
		soundGun.SetGun(this);
		soundGun.SetGunTransform(base.transform);
		soundGun.RefreshSoundModifiers();
	}

	private void Update()
	{
		if ((bool)holdable && (bool)holdable.holder && (bool)holdable.holder.player)
		{
			player = holdable.holder.player;
		}
		sinceAttack += TimeHandler.deltaTime * attackSpeedMultiplier;
		if (!GameManager.instance.battleOngoing || (player != null && (!player.data.isPlaying || player.data.dead)))
		{
			soundGun.StopAutoPlayTail();
		}
	}

	private void OnDestroy()
	{
		soundGun.StopAutoPlayTail();
	}

	public bool IsReady(float readuIn = 0f)
	{
		return sinceAttack + readuIn * attackSpeedMultiplier > usedCooldown;
	}

	public float ReadyAmount()
	{
		return sinceAttack / usedCooldown;
	}

	public override bool Attack(float charge, bool forceAttack = false, float damageM = 1f, float recoilM = 1f, bool useAmmo = true)
	{
		if (sinceAttack < usedCooldown && !forceAttack)
		{
			return false;
		}
		if (isReloading && !forceAttack)
		{
			return false;
		}
		sinceAttack = 0f;
		int attacks = Mathf.Clamp(Mathf.RoundToInt(0.5f * charge / attackSpeed), 1, 10);
		if (lockGunToDefault)
		{
			attacks = 1;
		}
		StartCoroutine(DoAttacks(charge, forceAttack, damageM, attacks, recoilM, useAmmo));
		return true;
	}

	private IEnumerator DoAttacks(float charge, bool forceAttack = false, float damageM = 1f, int attacks = 1, float recoilM = 1f, bool useAmmo = true)
	{
		for (int i = 0; i < attacks; i++)
		{
			DoAttack(charge, forceAttack, damageM, recoilM, useAmmo);
			yield return new WaitForSeconds(0.3f / (float)attacks);
		}
	}

	private void DoAttack(float charge, bool forceAttack = false, float damageM = 1f, float recoilM = 1f, bool useAmmo = true)
	{
		float num = 1f * (1f + charge * chargeRecoilTo) * recoilM;
		if ((bool)rig)
		{
			rig.AddForce(rig.mass * recoil * Mathf.Clamp(usedCooldown, 0f, 1f) * -base.transform.up, ForceMode2D.Impulse);
		}
		bool flag = (bool)holdable;
		if (attackAction != null)
		{
			attackAction();
		}
		StartCoroutine(FireBurst(charge, forceAttack, damageM, recoilM, useAmmo));
	}

	private bool CheckIsMine()
	{
		bool result = false;
		if ((bool)holdable && (bool)holdable.holder)
		{
			result = holdable.holder.player.data.view.IsMine;
		}
		else
		{
			Player componentInParent = GetComponentInParent<Player>();
			if ((bool)componentInParent)
			{
				result = componentInParent.data.view.IsMine;
			}
		}
		return result;
	}

	private IEnumerator FireBurst(float charge, bool forceAttack = false, float damageM = 1f, float recoilM = 1f, bool useAmmo = true)
	{
		int currentNumberOfProjectiles = (lockGunToDefault ? 1 : (numberOfProjectiles + Mathf.RoundToInt(chargeNumberOfProjectilesTo * charge)));
		if (!lockGunToDefault)
		{
			int num = currentNumberOfProjectiles;
		}
		if (timeBetweenBullets == 0f)
		{
			GamefeelManager.GameFeel(base.transform.up * shake);
			soundGun.PlayShot(currentNumberOfProjectiles);
		}
		for (int ii = 0; ii < Mathf.Clamp(bursts, 1, 100); ii++)
		{
			for (int i = 0; i < projectiles.Length; i++)
			{
				for (int j = 0; j < currentNumberOfProjectiles; j++)
				{
					if (CheckIsMine())
					{
						spawnPos = base.transform.position;
						if ((bool)player)
						{
							player.GetComponent<PlayerAudioModifyers>().SetStacks();
							if ((bool)holdable)
							{
								spawnPos = player.transform.position;
							}
						}
						GameObject gameObject = PhotonNetwork.Instantiate(projectiles[i].objectToSpawn.gameObject.name, spawnPos, getShootRotation(j, currentNumberOfProjectiles, charge), 0);
						if ((bool)holdable)
						{
							if (useAmmo)
							{
								if (PhotonNetwork.OfflineMode)
								{
									gameObject.GetComponent<ProjectileInit>().OFFLINE_Init(holdable.holder.player.playerID, currentNumberOfProjectiles, damageM, UnityEngine.Random.Range(0f, 1f));
								}
								else
								{
									gameObject.GetComponent<PhotonView>().RPC("RPCA_Init", RpcTarget.All, holdable.holder.view.OwnerActorNr, currentNumberOfProjectiles, damageM, UnityEngine.Random.Range(0f, 1f));
								}
							}
							else if (PhotonNetwork.OfflineMode)
							{
								gameObject.GetComponent<ProjectileInit>().OFFLINE_Init_noAmmoUse(holdable.holder.player.playerID, currentNumberOfProjectiles, damageM, UnityEngine.Random.Range(0f, 1f));
							}
							else
							{
								gameObject.GetComponent<PhotonView>().RPC("RPCA_Init_noAmmoUse", RpcTarget.All, holdable.holder.view.OwnerActorNr, currentNumberOfProjectiles, damageM, UnityEngine.Random.Range(0f, 1f));
							}
						}
						else if (PhotonNetwork.OfflineMode)
						{
							gameObject.GetComponent<ProjectileInit>().OFFLINE_Init_SeparateGun(GetComponentInParent<Player>().playerID, gunID, currentNumberOfProjectiles, damageM, UnityEngine.Random.Range(0f, 1f));
						}
						else
						{
							gameObject.GetComponent<PhotonView>().RPC("RPCA_Init_SeparateGun", RpcTarget.All, GetComponentInParent<CharacterData>().view.OwnerActorNr, gunID, currentNumberOfProjectiles, damageM, UnityEngine.Random.Range(0f, 1f));
						}
					}
					if (timeBetweenBullets != 0f)
					{
						GamefeelManager.GameFeel(base.transform.up * shake);
						soundGun.PlayShot(currentNumberOfProjectiles);
					}
				}
			}
			if (bursts > 1 && ii + 1 == Mathf.Clamp(bursts, 1, 100))
			{
				soundGun.StopAutoPlayTail();
			}
			if (timeBetweenBullets > 0f)
			{
				yield return new WaitForSeconds(timeBetweenBullets);
			}
		}
	}

	public void BulletInit(GameObject bullet, int usedNumberOfProjectiles, float damageM, float randomSeed, bool useAmmo = true)
	{
		spawnedAttack = bullet.GetComponent<SpawnedAttack>();
		if (!spawnedAttack)
		{
			spawnedAttack = bullet.AddComponent<SpawnedAttack>();
		}
		if (!bullet.GetComponentInChildren<DontChangeMe>())
		{
			ApplyProjectileStats(bullet, usedNumberOfProjectiles, damageM, randomSeed);
		}
		if (soundDisableRayHitBulletSound)
		{
			RayHitBulletSound component = bullet.GetComponent<RayHitBulletSound>();
			if (component != null)
			{
				component.disableImpact = true;
			}
		}
		ApplyPlayerStuff(bullet);
		if (ShootPojectileAction != null)
		{
			ShootPojectileAction(bullet);
		}
		if (useAmmo && (bool)gunAmmo)
		{
			gunAmmo.Shoot(bullet);
		}
	}

	private Quaternion getShootRotation(int bulletID, int numOfProj, float charge)
	{
		Vector3 forward = shootPosition.forward;
		if (forceShootDir != Vector3.zero)
		{
			forward = forceShootDir;
		}
		float num = multiplySpread * Mathf.Clamp(1f + charge * chargeSpreadTo, 0f, float.PositiveInfinity);
		float num2 = UnityEngine.Random.Range(0f - spread, spread);
		num2 /= (1f + projectileSpeed * 0.5f) * 0.5f;
		forward += Vector3.Cross(forward, Vector3.forward) * num2 * num;
		return Quaternion.LookRotation(lockGunToDefault ? shootPosition.forward : forward);
	}

	private void ApplyPlayerStuff(GameObject obj)
	{
		ProjectileHit component = obj.GetComponent<ProjectileHit>();
		component.ownWeapon = base.gameObject;
		if ((bool)player)
		{
			component.ownPlayer = player;
		}
		spawnedAttack.spawner = player;
		spawnedAttack.attackID = attackID;
	}

	private void ApplyProjectileStats(GameObject obj, int numOfProj = 1, float damageM = 1f, float randomSeed = 0f)
	{
		ProjectileHit component = obj.GetComponent<ProjectileHit>();
		component.dealDamageMultiplierr *= bulletDamageMultiplier;
		component.damage *= damage * damageM;
		component.percentageDamage = percentageDamage;
		component.stun = component.damage / 150f;
		component.force *= knockback;
		component.movementSlow = slow;
		component.hasControl = CheckIsMine();
		component.projectileColor = projectileColor;
		component.unblockable = unblockable;
		RayCastTrail component2 = obj.GetComponent<RayCastTrail>();
		if (ignoreWalls)
		{
			component2.mask = component2.ignoreWallsMask;
		}
		if ((bool)component2)
		{
			component2.extraSize += size;
		}
		if ((bool)player)
		{
			PlayerSkin teamColor = (component.team = PlayerSkinBank.GetPlayerSkinColors(player.playerID));
			obj.GetComponent<RayCastTrail>().teamID = player.playerID;
			SetTeamColor.TeamColorThis(obj, teamColor);
		}
		List<ObjectsToSpawn> list = new List<ObjectsToSpawn>();
		for (int i = 0; i < objectsToSpawn.Length; i++)
		{
			list.Add(objectsToSpawn[i]);
			if (!objectsToSpawn[i].AddToProjectile || ((bool)objectsToSpawn[i].AddToProjectile.gameObject.GetComponent<StopRecursion>() && isProjectileGun))
			{
				continue;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(objectsToSpawn[i].AddToProjectile, component.transform.position, component.transform.rotation, component.transform);
			gameObject.transform.localScale *= 1f * (1f - objectsToSpawn[i].scaleFromDamage) + component.damage / 55f * objectsToSpawn[i].scaleFromDamage;
			if (objectsToSpawn[i].scaleStacks)
			{
				gameObject.transform.localScale *= 1f + (float)objectsToSpawn[i].stacks * objectsToSpawn[i].scaleStackM;
			}
			if (!objectsToSpawn[i].removeScriptsFromProjectileObject)
			{
				continue;
			}
			MonoBehaviour[] componentsInChildren = gameObject.GetComponentsInChildren<MonoBehaviour>();
			for (int j = 0; j < componentsInChildren.Length; j++)
			{
				if (componentsInChildren[j].GetType().ToString() != "SoundImplementation.SoundUnityEventPlayer")
				{
					UnityEngine.Object.Destroy(componentsInChildren[j]);
				}
				Debug.Log(componentsInChildren[j].GetType().ToString());
			}
		}
		component.objectsToSpawn = list.ToArray();
		if (reflects > 0)
		{
			RayHitReflect rayHitReflect = obj.gameObject.AddComponent<RayHitReflect>();
			rayHitReflect.reflects = reflects;
			rayHitReflect.speedM = speedMOnBounce;
			rayHitReflect.dmgM = dmgMOnBounce;
		}
		if (!forceSpecificShake)
		{
			float num = component.damage / 100f * ((1f + usedCooldown) / 2f) / ((1f + (float)numOfProj) / 2f) * 2f;
			float num2 = Mathf.Clamp((0.2f + component.damage * (((float)numberOfProjectiles + 2f) / 2f) / 100f * ((1f + usedCooldown) / 2f)) * 1f, 0f, 3f);
			component.shake = num * shakeM;
			shake = num2;
		}
		MoveTransform component3 = obj.GetComponent<MoveTransform>();
		component3.localForce *= projectileSpeed;
		component3.simulationSpeed *= projectielSimulatonSpeed;
		component3.gravity *= gravity;
		component3.worldForce *= gravity;
		component3.drag = drag;
		component3.drag = Mathf.Clamp(component3.drag, 0f, 45f);
		component3.velocitySpread = Mathf.Clamp(spread * 50f, 0f, 50f);
		component3.dragMinSpeed = dragMinSpeed;
		component3.localForce *= Mathf.Lerp(1f - component3.velocitySpread * 0.01f, 1f + component3.velocitySpread * 0.01f, randomSeed);
		component3.selectedSpread = 0f;
		if (damageAfterDistanceMultiplier != 1f)
		{
			obj.AddComponent<ChangeDamageMultiplierAfterDistanceTravelled>().muiltiplier = damageAfterDistanceMultiplier;
		}
		if (cos > 0f)
		{
			obj.gameObject.AddComponent<Cos>().multiplier = cos;
		}
		if (destroyBulletAfter != 0f)
		{
			obj.GetComponent<RemoveAfterSeconds>().seconds = destroyBulletAfter;
		}
		if ((bool)spawnedAttack && projectileColor != Color.black)
		{
			spawnedAttack.SetColor(projectileColor);
		}
	}

	public void AddAttackAction(Action action)
	{
		attackAction = (Action)Delegate.Combine(attackAction, action);
	}

	internal void RemoveAttackAction(Action action)
	{
		attackAction = (Action)Delegate.Remove(attackAction, action);
	}
}
