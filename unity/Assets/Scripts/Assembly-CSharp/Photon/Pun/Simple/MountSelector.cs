using System;

namespace Photon.Pun.Simple
{
	[Serializable]
	public struct MountSelector
	{
		public int id;

		public MountSelector(int index)
		{
			id = index;
		}

		public static implicit operator int(MountSelector selector)
		{
			return selector.id;
		}

		public static implicit operator MountSelector(int id)
		{
			return new MountSelector(id);
		}
	}
}
