using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class ObjectsToSpawn
{
	public enum Direction
	{
		forward = 0,
		normal = 1,
		identity = 2
	}

	public enum SpawnOn
	{
		all = 0,
		player = 1,
		notPlayer = 2
	}

	[FoldoutGroup("OnHit", 0)]
	public GameObject effect;

	[FoldoutGroup("OnHit", 0)]
	public Direction direction;

	[FoldoutGroup("OnHit", 0)]
	public SpawnOn spawnOn;

	[FoldoutGroup("OnHit", 0)]
	public bool spawnAsChild;

	[FoldoutGroup("OnHit", 0)]
	public int numberOfSpawns = 1;

	[FoldoutGroup("OnHit", 0)]
	public float normalOffset;

	[FoldoutGroup("OnHit", 0)]
	public bool stickToBigTargets;

	[FoldoutGroup("OnHit", 0)]
	public bool stickToAllTargets;

	[FoldoutGroup("OnHit", 0)]
	public bool zeroZ;

	[FoldoutGroup("OnProjectile", 0)]
	public GameObject AddToProjectile;

	[FoldoutGroup("OnProjectile", 0)]
	public bool removeScriptsFromProjectileObject;

	[FoldoutGroup("Stacking", 0)]
	public bool scaleStacks;

	[FoldoutGroup("Stacking", 0)]
	public float scaleStackM = 0.5f;

	[FoldoutGroup("Scaling", 0)]
	public float scaleFromDamage;

	[HideInInspector]
	public int stacks;

	public static GameObject[] SpawnObject(Transform spawnerTransform, HitInfo hit, ObjectsToSpawn objectToSpawn, HealthHandler playerHealth, PlayerSkin playerSkins, float damage = 55f, SpawnedAttack spawnedAttack = null, bool wasBlocked = false)
	{
		GameObject[] array = new GameObject[objectToSpawn.numberOfSpawns];
		for (int i = 0; i < objectToSpawn.numberOfSpawns; i++)
		{
			if (wasBlocked && objectToSpawn.stickToAllTargets)
			{
				continue;
			}
			Vector3 position = (Vector3)hit.point + (Vector3)hit.normal * objectToSpawn.normalOffset + (objectToSpawn.zeroZ ? Vector3.zero : (Vector3.forward * 5f));
			Quaternion rotation = Quaternion.LookRotation(spawnerTransform.forward);
			if (objectToSpawn.direction == Direction.normal)
			{
				rotation = Quaternion.LookRotation(hit.normal + Vector2.right * 0.005f);
			}
			if (objectToSpawn.direction == Direction.identity)
			{
				rotation = Quaternion.identity;
			}
			if ((objectToSpawn.spawnOn != SpawnOn.notPlayer || !playerHealth) && (objectToSpawn.spawnOn != SpawnOn.player || (bool)playerHealth) && (bool)objectToSpawn.effect)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(objectToSpawn.effect, position, rotation);
				if (objectToSpawn.spawnAsChild && (bool)hit.transform)
				{
					gameObject.transform.SetParent(hit.transform, true);
				}
				if ((bool)spawnedAttack)
				{
					spawnedAttack.CopySpawnedAttackTo(gameObject);
				}
				array[i] = gameObject;
				SetTeamColor.TeamColorThis(gameObject, playerSkins);
				if ((objectToSpawn.stickToBigTargets && !playerHealth && (!hit.rigidbody || hit.rigidbody.mass > 500f)) || objectToSpawn.stickToAllTargets)
				{
					gameObject.AddComponent<FollowLocalPos>().Follow(hit.transform);
				}
				if (objectToSpawn.scaleFromDamage != 0f)
				{
					gameObject.transform.localScale *= 1f * (1f - objectToSpawn.scaleFromDamage) + damage / 55f * objectToSpawn.scaleFromDamage;
				}
				if (objectToSpawn.scaleStacks)
				{
					gameObject.transform.localScale *= 1f + (float)objectToSpawn.stacks * objectToSpawn.scaleStackM;
				}
			}
		}
		return array;
	}

	public static void SpawnObject(ObjectsToSpawn objectToSpawn, Vector3 position, Quaternion rotation)
	{
		for (int i = 0; i < objectToSpawn.numberOfSpawns; i++)
		{
			UnityEngine.Object.Instantiate(objectToSpawn.effect, position, rotation);
		}
	}
}
