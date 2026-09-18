using System;
using System.Collections.Generic;
using Photon.Utilities;
using UnityEngine;

namespace Photon.Pun.Simple;

[AddComponentMenu("")]
[DisallowMultipleComponent]
public class MountsManager : MonoBehaviour, IOnAwake, IOnPreQuit
{
	[NonSerialized]
	public Dictionary<int, Mount> mountIdLookup = new Dictionary<int, Mount>();

	[NonSerialized]
	public List<Mount> indexedMounts = new List<Mount>();

	[NonSerialized]
	public int bitsForMountId;

	public void OnAwake()
	{
		CollectMounts();
		bitsForMountId = ((indexedMounts.Count != 0) ? (indexedMounts.Count - 1).GetBitsForMaxValue() : 0);
	}

	public void OnPreQuit()
	{
		UnmountAll();
	}

	public List<Mount> CollectMounts(bool force = false)
	{
		indexedMounts.Clear();
		mountIdLookup.Clear();
		base.transform.GetNestedComponentsInChildren<Mount, NetObject>(indexedMounts);
		int i = 0;
		for (int count = indexedMounts.Count; i < count; i++)
		{
			Mount mount = indexedMounts[i];
			mount.componentIndex = i;
			int id = mount.mountType.id;
			if (!mountIdLookup.ContainsKey(id))
			{
				mountIdLookup.Add(id, mount);
			}
		}
		return indexedMounts;
	}

	public Mount GetMount(int mountId)
	{
		mountIdLookup.TryGetValue(mountId, out var value);
		return value;
	}

	public void UnmountAll()
	{
		int i = 0;
		for (int count = indexedMounts.Count; i < count; i++)
		{
			indexedMounts[i].DismountAll();
		}
	}
}
