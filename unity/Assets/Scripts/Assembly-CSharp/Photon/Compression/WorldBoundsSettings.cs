using System.Collections.Generic;
using Photon.Utilities;
using UnityEngine;
using emotitron.Compression;

namespace Photon.Compression
{
	[CreateAssetMenu]
	public class WorldBoundsSettings : SettingsScriptableObject<WorldBoundsSettings>
	{
		[HideInInspector]
		public List<WorldBoundsGroup> worldBoundsGroups = new List<WorldBoundsGroup>();

		public static ElementCrusher defaultWorldBoundsCrusher;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Bootstrap()
		{
			WorldBoundsSettings worldBoundsSettings = SettingsScriptableObject<WorldBoundsSettings>.Single;
			List<WorldBoundsGroup> list = worldBoundsSettings.worldBoundsGroups;
			if (worldBoundsSettings != null && list.Count > 0)
			{
				defaultWorldBoundsCrusher = worldBoundsSettings.worldBoundsGroups[0].crusher;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			if (worldBoundsGroups.Count == 0)
			{
				worldBoundsGroups.Add(new WorldBoundsGroup());
			}
		}

		public override void Initialize()
		{
			base.Initialize();
			if (worldBoundsGroups.Count == 0)
			{
				worldBoundsGroups.Add(new WorldBoundsGroup());
			}
			defaultWorldBoundsCrusher = worldBoundsGroups[0].crusher;
			foreach (WorldBoundsGroup worldBoundsGroup in worldBoundsGroups)
			{
				worldBoundsGroup.RecalculateWorldCombinedBounds();
			}
		}

		public static void RemoveWorldBoundsFromAll(WorldBounds wb)
		{
			List<WorldBoundsGroup> list = SettingsScriptableObject<WorldBoundsSettings>.Single.worldBoundsGroups;
			for (int i = 0; i < list.Count; i++)
			{
				List<WorldBounds> activeWorldBounds = list[i].activeWorldBounds;
				if (activeWorldBounds.Contains(wb))
				{
					activeWorldBounds.Remove(wb);
					list[i].RecalculateWorldCombinedBounds();
				}
			}
		}

		public static int TallyBits(ref int index, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			List<WorldBoundsGroup> list = SettingsScriptableObject<WorldBoundsSettings>.Single.worldBoundsGroups;
			if (index >= list.Count)
			{
				index = 0;
			}
			ElementCrusher crusher = SettingsScriptableObject<WorldBoundsSettings>.single.worldBoundsGroups[index].crusher;
			return crusher.XCrusher.GetBits(bcl) + crusher.YCrusher.GetBits(bcl) + crusher.ZCrusher.GetBits(bcl);
		}
	}
}
