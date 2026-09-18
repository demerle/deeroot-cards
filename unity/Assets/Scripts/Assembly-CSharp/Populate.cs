using System.Collections.Generic;
using UnityEngine;

public class Populate : MonoBehaviour
{
	public GameObject target;

	public bool setTargetsActive;

	public bool includeTargetInList = true;

	public int times = 5;

	public List<GameObject> DoPopulate()
	{
		List<GameObject> list = new List<GameObject>();
		if (includeTargetInList)
		{
			list.Add(target);
		}
		for (int i = 0; i < times; i++)
		{
			GameObject gameObject = Object.Instantiate(target, target.transform.position, base.transform.transform.rotation, base.transform);
			gameObject.transform.localScale = target.transform.localScale;
			list.Add(gameObject);
			if (setTargetsActive)
			{
				gameObject.SetActive(true);
			}
		}
		return list;
	}

	public List<T> DoPopulate<T>(bool addComponentIfMissing = true) where T : MonoBehaviour
	{
		List<T> list = new List<T>();
		if (includeTargetInList && target != null)
		{
			T val = target.GetComponent<T>();
			if ((Object)val == (Object)null && addComponentIfMissing)
			{
				val = target.AddComponent<T>();
			}
			if (!((Object)val != (Object)null))
			{
				Debug.LogError("Could not find component");
				return null;
			}
			list.Add(val);
		}
		for (int i = 0; i < times; i++)
		{
			GameObject gameObject = Object.Instantiate(target, target.transform.position, base.transform.transform.rotation, target.transform.transform.parent);
			gameObject.transform.localScale = target.transform.localScale;
			T val2 = gameObject.GetComponent<T>();
			if ((Object)val2 == (Object)null && addComponentIfMissing)
			{
				val2 = gameObject.AddComponent<T>();
			}
			if ((Object)val2 != (Object)null)
			{
				list.Add(val2);
			}
			if (setTargetsActive)
			{
				gameObject.SetActive(true);
			}
		}
		return list;
	}
}
