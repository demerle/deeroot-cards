using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
	private Stack<GameObject> m_availableObjects = new Stack<GameObject>();

	private List<GameObject> m_usedObjects = new List<GameObject>();

	private GameObject m_objectPrefab;

	private Transform m_objectRoot;

	private int testCounter = 1;

	public ObjectPool(GameObject prefab, int initSpawn = 0, Transform parent = null)
	{
		m_objectPrefab = prefab;
		m_objectRoot = new GameObject(prefab.name + "_root").transform;
		m_objectRoot.transform.position = parent.position;
		m_objectRoot.transform.rotation = parent.rotation;
		m_objectRoot.transform.SetParent(parent, worldPositionStays: true);
		m_objectRoot.transform.localScale = Vector3.one;
		for (int i = 0; i < initSpawn; i++)
		{
			GameObject gameObject = Object.Instantiate(m_objectPrefab, m_objectRoot);
			gameObject.SetActive(value: false);
			m_availableObjects.Push(gameObject);
		}
	}

	public GameObject GetObject()
	{
		if (m_availableObjects.Count > 0)
		{
			GameObject gameObject = m_availableObjects.Pop();
			m_usedObjects.Add(gameObject);
			gameObject.SetActive(value: true);
			return gameObject;
		}
		GameObject gameObject2 = Object.Instantiate(m_objectPrefab, m_objectRoot);
		m_usedObjects.Add(gameObject2);
		gameObject2.SetActive(value: true);
		return gameObject2;
	}

	public bool ReleaseObject(GameObject obj)
	{
		bool num = m_usedObjects.Remove(obj);
		if (num)
		{
			m_availableObjects.Push(obj);
			obj.SetActive(value: false);
		}
		return num;
	}

	public void ClearPool()
	{
		while (m_availableObjects.Count > 0)
		{
			Object.Destroy(m_availableObjects.Pop());
		}
		for (int i = 0; i < m_usedObjects.Count; i++)
		{
			Object.Destroy(m_usedObjects[i]);
		}
		m_usedObjects.Clear();
	}
}
