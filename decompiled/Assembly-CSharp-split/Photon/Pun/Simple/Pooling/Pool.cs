using System;
using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.Simple.Pooling;

public class Pool : MonoBehaviour
{
	private struct PoolItemDef
	{
		public GameObject prefab;

		public int growBy;

		public Type scriptToAdd;

		public PoolItemDef(GameObject prefab, int growBy, Type scriptToAdd)
		{
			this.prefab = prefab;
			this.growBy = growBy;
			this.scriptToAdd = scriptToAdd;
		}
	}

	private static Dictionary<GameObject, Stack<Pool>> pools = new Dictionary<GameObject, Stack<Pool>>();

	private static Dictionary<GameObject, PoolItemDef> poolItemDefs = new Dictionary<GameObject, PoolItemDef>();

	[HideInInspector]
	public GameObject origPrefab;

	[HideInInspector]
	public Rigidbody rb;

	[HideInInspector]
	public Component extraScript;

	public float deathClock;

	public static void AddPrefabToPool(GameObject _prefab, int startingSize = 8, int _growBy = 8, Type _scriptToAdd = null, bool tidyUp = false)
	{
		if (!poolItemDefs.ContainsKey(_prefab))
		{
			pools.Add(_prefab, new Stack<Pool>());
			PoolItemDef value = new PoolItemDef(_prefab, _growBy, _scriptToAdd);
			poolItemDefs.Add(_prefab, value);
			GrowPool(_prefab, startingSize);
			if (tidyUp)
			{
				_prefab.gameObject.SetActive(value: false);
			}
		}
	}

	public static Pool Spawn(GameObject origPrefab, Transform t, float duration = 5f)
	{
		return Spawn(origPrefab, t.position, t.rotation, duration);
	}

	public static Pool Spawn(GameObject origPrefab, Vector3 pos, Quaternion rot, Vector3 scl, float duration = 5f)
	{
		Pool pool = Spawn(origPrefab, pos, rot, duration);
		pool.gameObject.transform.localScale = scl;
		return pool;
	}

	public static Pool Spawn(GameObject origPrefab, Vector3 pos, Quaternion rot, float duration = 5f)
	{
		if (pools[origPrefab].Count == 0)
		{
			GrowPool(origPrefab);
		}
		if (!pools[origPrefab].Peek())
		{
			pools[origPrefab].Clear();
			GrowPool(origPrefab);
		}
		Pool pool = pools[origPrefab].Pop();
		pool.transform.position = pos;
		pool.transform.rotation = rot;
		pool.deathClock = duration;
		pool.enabled = duration > 0f;
		pool.gameObject.SetActive(value: true);
		return pool;
	}

	private static void GrowPool(GameObject _prefab, int growAmt = -1)
	{
		PoolItemDef def = poolItemDefs[_prefab];
		int num = ((growAmt < 1) ? def.growBy : growAmt);
		for (int i = 0; i < num; i++)
		{
			AddItemToPool(UnityEngine.Object.Instantiate(def.prefab), _prefab, def);
		}
	}

	private static void AddItemToPool(GameObject go, GameObject _prefab, PoolItemDef def)
	{
		go.SetActive(value: false);
		Pool pool = go.AddComponent<Pool>();
		pool.CacheComponents();
		if (def.scriptToAdd != null && go.GetComponent(def.scriptToAdd) == null)
		{
			pool.extraScript = go.AddComponent(def.scriptToAdd);
		}
		pool.origPrefab = _prefab;
		pools[_prefab].Push(pool);
	}

	private static void ReturnToPool(Pool p, GameObject _prefab)
	{
		pools[_prefab].Push(p);
	}

	public void CacheComponents()
	{
		rb = GetComponent<Rigidbody>();
	}

	private void Update()
	{
		deathClock -= TimeHandler.deltaTime;
		if (deathClock < 0f)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	private void OnDisable()
	{
		ReturnToPool(this, origPrefab);
	}
}
