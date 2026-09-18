using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AmplifyColor
{
	[Serializable]
	public class VolumeEffectFlags
	{
		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass2_0
		{
			public Component c;

			internal bool _003CAddComponent_003Eb__0(VolumeEffectComponentFlags s)
			{
				return s.componentName == string.Concat(c.GetType());
			}
		}

		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass3_0
		{
			public VolumeEffectComponent comp;

			internal bool _003CUpdateFlags_003Eb__0(VolumeEffectComponentFlags s)
			{
				return s.componentName == comp.componentName;
			}
		}

		[Serializable]
		[CompilerGenerated]
		private sealed class _003C_003Ec
		{
			public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

			public static Func<VolumeEffectComponentFlags, bool> _003C_003E9__7_0;

			public static Func<VolumeEffectComponentFlags, string> _003C_003E9__7_1;

			internal bool _003CGetComponentNames_003Eb__7_0(VolumeEffectComponentFlags r)
			{
				return r.blendFlag;
			}

			internal string _003CGetComponentNames_003Eb__7_1(VolumeEffectComponentFlags r)
			{
				return r.componentName;
			}
		}

		public List<VolumeEffectComponentFlags> components;

		public VolumeEffectFlags()
		{
			components = new List<VolumeEffectComponentFlags>();
		}

		public void AddComponent(Component c)
		{
			_003C_003Ec__DisplayClass2_0 _003C_003Ec__DisplayClass2_ = new _003C_003Ec__DisplayClass2_0();
			_003C_003Ec__DisplayClass2_.c = c;
			VolumeEffectComponentFlags volumeEffectComponentFlags;
			if ((volumeEffectComponentFlags = components.Find(_003C_003Ec__DisplayClass2_._003CAddComponent_003Eb__0)) != null)
			{
				volumeEffectComponentFlags.UpdateComponentFlags(_003C_003Ec__DisplayClass2_.c);
			}
			else
			{
				components.Add(new VolumeEffectComponentFlags(_003C_003Ec__DisplayClass2_.c));
			}
		}

		public void UpdateFlags(VolumeEffect effectVol)
		{
			using (List<VolumeEffectComponent>.Enumerator enumerator = effectVol.components.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					_003C_003Ec__DisplayClass3_0 _003C_003Ec__DisplayClass3_ = new _003C_003Ec__DisplayClass3_0();
					_003C_003Ec__DisplayClass3_.comp = enumerator.Current;
					VolumeEffectComponentFlags volumeEffectComponentFlags = null;
					if ((volumeEffectComponentFlags = components.Find(_003C_003Ec__DisplayClass3_._003CUpdateFlags_003Eb__0)) == null)
					{
						components.Add(new VolumeEffectComponentFlags(_003C_003Ec__DisplayClass3_.comp));
					}
					else
					{
						volumeEffectComponentFlags.UpdateComponentFlags(_003C_003Ec__DisplayClass3_.comp);
					}
				}
			}
		}

		public static void UpdateCamFlags(AmplifyColorBase[] effects, AmplifyColorVolumeBase[] volumes)
		{
			foreach (AmplifyColorBase amplifyColorBase in effects)
			{
				amplifyColorBase.EffectFlags = new VolumeEffectFlags();
				for (int j = 0; j < volumes.Length; j++)
				{
					VolumeEffect volumeEffect = volumes[j].EffectContainer.FindVolumeEffect(amplifyColorBase);
					if (volumeEffect != null)
					{
						amplifyColorBase.EffectFlags.UpdateFlags(volumeEffect);
					}
				}
			}
		}

		public VolumeEffect GenerateEffectData(AmplifyColorBase go)
		{
			VolumeEffect volumeEffect = new VolumeEffect(go);
			foreach (VolumeEffectComponentFlags component2 in components)
			{
				if (component2.blendFlag)
				{
					Component component = go.GetComponent(component2.componentName);
					if (component != null)
					{
						volumeEffect.AddComponent(component, component2);
					}
				}
			}
			return volumeEffect;
		}

		public VolumeEffectComponentFlags FindComponentFlags(string compName)
		{
			for (int i = 0; i < components.Count; i++)
			{
				if (components[i].componentName == compName)
				{
					return components[i];
				}
			}
			return null;
		}

		public string[] GetComponentNames()
		{
			return components.Where(_003C_003Ec._003C_003E9__7_0 ?? (_003C_003Ec._003C_003E9__7_0 = _003C_003Ec._003C_003E9._003CGetComponentNames_003Eb__7_0)).Select(_003C_003Ec._003C_003E9__7_1 ?? (_003C_003Ec._003C_003E9__7_1 = _003C_003Ec._003C_003E9._003CGetComponentNames_003Eb__7_1)).ToArray();
		}
	}
}
