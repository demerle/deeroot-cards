using System;
using UnityEngine;

namespace Photon.Pun.Simple.ContactGroups;

[Serializable]
public struct ContactGroupMaskSelector : IContactGroupMask
{
	[SerializeField]
	private int mask;

	public int Mask
	{
		get
		{
			return mask;
		}
		set
		{
			mask = value;
		}
	}

	public ContactGroupMaskSelector(int mask)
	{
		this.mask = mask;
	}

	public static implicit operator int(ContactGroupMaskSelector selector)
	{
		return selector.mask;
	}

	public static implicit operator ContactGroupMaskSelector(int mask)
	{
		return new ContactGroupMaskSelector(mask);
	}
}
