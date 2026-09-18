using System.Collections.Generic;
using Photon.Utilities;
using UnityEngine;

namespace Photon.Pun.Simple
{
	public class MountSettings : SettingsScriptableObject<MountSettings>
	{
		[HideInInspector]
		[SerializeField]
		private List<string> mountNames = new List<string> { "Root", "1", "2", "3", "4" };

		public static int mountTypeCount;

		public static int bitsForMountId;

		public static int AllTrueMask
		{
			get
			{
				if (SettingsScriptableObject<MountSettings>.Single.mountNames.Count == 32)
				{
					return -1;
				}
				return (int)((1L << SettingsScriptableObject<MountSettings>.Single.mountNames.Count) - 1);
			}
		}

		public override void Initialize()
		{
			base.Initialize();
			mountTypeCount = SettingsScriptableObject<MountSettings>.Single.mountNames.Count;
			bitsForMountId = (mountTypeCount - 1).GetBitsForMaxValue();
		}

		public static int GetIndex(string name)
		{
			return SettingsScriptableObject<MountSettings>.single.mountNames.IndexOf(name);
		}

		public static string GetName(int index)
		{
			if (index >= mountTypeCount)
			{
				return null;
			}
			return SettingsScriptableObject<MountSettings>.single.mountNames[index];
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Bootstrap()
		{
			MountSettings single2 = SettingsScriptableObject<MountSettings>.Single;
		}
	}
}
