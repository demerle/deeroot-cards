using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AmplifyColor
{
	[Serializable]
	public class VolumeEffectContainer
	{
		[Serializable]
		[CompilerGenerated]
		private sealed class _003C_003Ec
		{
			public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

			public static Func<VolumeEffect, AmplifyColorBase> _003C_003E9__6_0;

			internal AmplifyColorBase _003CGetStoredEffects_003Eb__6_0(VolumeEffect r)
			{
				return r.gameObject;
			}
		}

		public List<VolumeEffect> volumes;

		public VolumeEffectContainer()
		{
			volumes = new List<VolumeEffect>();
		}

		public void AddColorEffect(AmplifyColorBase colorEffect)
		{
			VolumeEffect volumeEffect;
			if ((volumeEffect = FindVolumeEffect(colorEffect)) != null)
			{
				volumeEffect.UpdateVolume();
				return;
			}
			volumeEffect = new VolumeEffect(colorEffect);
			volumes.Add(volumeEffect);
			volumeEffect.UpdateVolume();
		}

		public VolumeEffect AddJustColorEffect(AmplifyColorBase colorEffect)
		{
			VolumeEffect volumeEffect = new VolumeEffect(colorEffect);
			volumes.Add(volumeEffect);
			return volumeEffect;
		}

		public VolumeEffect FindVolumeEffect(AmplifyColorBase colorEffect)
		{
			for (int i = 0; i < volumes.Count; i++)
			{
				if (volumes[i].gameObject == colorEffect)
				{
					return volumes[i];
				}
			}
			for (int j = 0; j < volumes.Count; j++)
			{
				if (volumes[j].gameObject != null && volumes[j].gameObject.SharedInstanceID == colorEffect.SharedInstanceID)
				{
					return volumes[j];
				}
			}
			return null;
		}

		public void RemoveVolumeEffect(VolumeEffect volume)
		{
			volumes.Remove(volume);
		}

		public AmplifyColorBase[] GetStoredEffects()
		{
			return volumes.Select(_003C_003Ec._003C_003E9__6_0 ?? (_003C_003Ec._003C_003E9__6_0 = _003C_003Ec._003C_003E9._003CGetStoredEffects_003Eb__6_0)).ToArray();
		}
	}
}
