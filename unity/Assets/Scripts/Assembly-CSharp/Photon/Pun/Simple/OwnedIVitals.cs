using System.Collections.Generic;

namespace Photon.Pun.Simple
{
	public static class OwnedIVitals
	{
		public static List<IVitalsSystem> ownedVitalComponents = new List<IVitalsSystem>();

		public static List<IOnChangeOwnedVitals> iOnChangeOwnedVitals = new List<IOnChangeOwnedVitals>();

		public static IVitalsSystem LastItem
		{
			get
			{
				int count = ownedVitalComponents.Count;
				if (count <= 0)
				{
					return null;
				}
				return ownedVitalComponents[count - 1];
			}
		}

		public static void OnChangeAuthority(IVitalsSystem ivc, bool isMine, bool asServer)
		{
			if (isMine)
			{
				if (!ownedVitalComponents.Contains(ivc))
				{
					ownedVitalComponents.Add(ivc);
					for (int i = 0; i < iOnChangeOwnedVitals.Count; i++)
					{
						iOnChangeOwnedVitals[i].OnChangeOwnedVitals(ivc, null);
					}
				}
			}
			else if (ownedVitalComponents.Contains(ivc))
			{
				ownedVitalComponents.Remove(ivc);
				for (int j = 0; j < iOnChangeOwnedVitals.Count; j++)
				{
					iOnChangeOwnedVitals[j].OnChangeOwnedVitals(null, ivc);
				}
			}
		}
	}
}
