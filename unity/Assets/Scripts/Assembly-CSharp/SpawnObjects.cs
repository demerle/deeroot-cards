using System;
using Photon.Pun;
using UnityEngine;

public class SpawnObjects : MonoBehaviour
{
	public enum SpawnRot
	{
		Identity = 0,
		TransformRotation = 1
	}

	public GameObject[] objectToSpawn;

	public SpawnRot spawnRot;

	public bool inheritScale;

	public bool destroyObject;

	public bool destroyRoot;

	private PhotonView view;

	public Action<GameObject> SpawnedAction;

	[HideInInspector]
	public GameObject mostRecentlySpawnedObject;

	private void ConfigureObject(GameObject go)
	{
		SpawnedAttack spawnedAttack = go.GetComponent<SpawnedAttack>();
		if (!spawnedAttack)
		{
			spawnedAttack = go.AddComponent<SpawnedAttack>();
		}
		spawnedAttack.spawner = base.transform.root.GetComponent<Player>();
		if (!spawnedAttack.spawner)
		{
			SpawnedAttack componentInParent = base.transform.GetComponentInParent<SpawnedAttack>();
			if ((bool)componentInParent)
			{
				componentInParent.CopySpawnedAttackTo(go);
			}
		}
		AttackLevel componentInParent2 = GetComponentInParent<AttackLevel>();
		if ((bool)componentInParent2)
		{
			spawnedAttack.attackLevel = componentInParent2.attackLevel;
		}
		if (inheritScale)
		{
			go.transform.localScale *= base.transform.localScale.x;
		}
		if (SpawnedAction != null)
		{
			SpawnedAction(go);
		}
	}

	public void Spawn()
	{
		for (int i = 0; i < objectToSpawn.Length; i++)
		{
			Quaternion rotation = Quaternion.identity;
			if (spawnRot == SpawnRot.TransformRotation)
			{
				rotation = base.transform.rotation;
			}
			GameObject go = UnityEngine.Object.Instantiate(objectToSpawn[i], base.transform.position, rotation);
			ConfigureObject(go);
			mostRecentlySpawnedObject = go;
		}
		if (destroyObject)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		if (destroyRoot)
		{
			UnityEngine.Object.Destroy(base.transform.root.gameObject);
		}
	}
}
