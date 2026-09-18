using System;

namespace Photon.Pun.Simple;

[Serializable]
public struct MountMaskSelector
{
	public int mask;

	public MountMaskSelector(int mountTypeMask)
	{
		mask = mountTypeMask;
	}

	public MountMaskSelector(bool allTrue)
	{
		mask = (allTrue ? MountSettings.AllTrueMask : 0);
	}

	public static implicit operator int(MountMaskSelector selector)
	{
		return selector.mask;
	}

	public static implicit operator MountMaskSelector(int mask)
	{
		return new MountMaskSelector(mask);
	}
}
