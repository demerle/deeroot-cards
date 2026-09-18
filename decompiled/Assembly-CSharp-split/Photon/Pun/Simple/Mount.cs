using System;
using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.Simple;

public class Mount : NetComponent, IOnPreQuit, IOnPreNetDestroy
{
	public const string ROOT_MOUNT_NAME = "Root";

	[Tooltip("A Mount component can be associated with more than one mount name. The first root will always include 'Root'.")]
	[SerializeField]
	[HideInInspector]
	public MountSelector mountType = new MountSelector(1);

	[SerializeField]
	[HideInInspector]
	public int componentIndex;

	[NonSerialized]
	public List<IMountable> mountedObjs = new List<IMountable>();

	[NonSerialized]
	public MountsManager mountsLookup;

	public override void OnAwake()
	{
		base.OnAwake();
		mountsLookup = netObj.GetComponent<MountsManager>();
	}

	public void OnPreQuit()
	{
		DismountAll();
	}

	public void OnPreNetDestroy(NetObject rootNetObj)
	{
		if (rootNetObj == netObj)
		{
			DismountAll();
		}
	}

	public void DismountAll()
	{
		for (int num = mountedObjs.Count - 1; num >= 0; num--)
		{
			if ((bool)(mountedObjs[num] as Component))
			{
				mountedObjs[num].ImmediateUnmount();
			}
		}
	}

	public static void ChangeMounting(IMountable mountable, Mount prevMount, Mount newMount)
	{
		prevMount?.mountedObjs.Remove(mountable);
		if ((object)newMount != null)
		{
			List<IMountable> list = newMount.mountedObjs;
			if (!list.Contains(mountable))
			{
				list.Add(mountable);
			}
		}
	}
}
