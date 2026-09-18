using System;

namespace Photon.Pun.Simple.ContactGroups;

[Serializable]
public struct ContactGroupSelector : IContactGroupMask
{
	public int index;

	public int Mask
	{
		get
		{
			if (index != 0)
			{
				return 1 << index - 1;
			}
			return 0;
		}
	}
}
