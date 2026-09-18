using System;
using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.Simple;

public class ContactManager : MonoBehaviour
{
	public List<IContactSystem> contactSystems = new List<IContactSystem>(0);

	public List<IContactTrigger> contactTriggers = new List<IContactTrigger>(0);

	public void Awake()
	{
		base.transform.GetNestedComponentsInChildren<IContactSystem, NetObject>(contactSystems);
		base.transform.GetNestedComponentsInChildren<IContactTrigger, NetObject>(contactTriggers);
		int count = contactSystems.Count;
		if (count > 255)
		{
			throw new IndexOutOfRangeException("NetObjects may not have more than 255 IContactSystem components on them.");
		}
		for (byte b = 0; b < count; b++)
		{
			contactSystems[b].SystemIndex = b;
		}
	}

	public IContactSystem GetContacting(int index)
	{
		return contactSystems[index];
	}
}
