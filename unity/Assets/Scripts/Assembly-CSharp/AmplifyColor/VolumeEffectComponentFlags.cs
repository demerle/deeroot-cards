using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AmplifyColor
{
	[Serializable]
	public class VolumeEffectComponentFlags
	{
		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass6_0
		{
			public VolumeEffectField field;

			internal bool _003CUpdateComponentFlags_003Eb__0(VolumeEffectFieldFlags s)
			{
				return s.fieldName == field.fieldName;
			}
		}

		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass7_0
		{
			public FieldInfo pi;

			internal bool _003CUpdateComponentFlags_003Eb__0(VolumeEffectFieldFlags s)
			{
				return s.fieldName == pi.Name;
			}
		}

		[Serializable]
		[CompilerGenerated]
		private sealed class _003C_003Ec
		{
			public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

			public static Func<VolumeEffectFieldFlags, bool> _003C_003E9__8_0;

			public static Func<VolumeEffectFieldFlags, string> _003C_003E9__8_1;

			internal bool _003CGetFieldNames_003Eb__8_0(VolumeEffectFieldFlags r)
			{
				return r.blendFlag;
			}

			internal string _003CGetFieldNames_003Eb__8_1(VolumeEffectFieldFlags r)
			{
				return r.fieldName;
			}
		}

		public string componentName;

		public List<VolumeEffectFieldFlags> componentFields;

		public bool blendFlag;

		public VolumeEffectComponentFlags(string name)
		{
			componentName = name;
			componentFields = new List<VolumeEffectFieldFlags>();
		}

		public VolumeEffectComponentFlags(VolumeEffectComponent comp)
			: this(comp.componentName)
		{
			blendFlag = true;
			foreach (VolumeEffectField field in comp.fields)
			{
				if (VolumeEffectField.IsValidType(field.fieldType))
				{
					componentFields.Add(new VolumeEffectFieldFlags(field));
				}
			}
		}

		public VolumeEffectComponentFlags(Component c)
			: this(string.Concat(c.GetType()))
		{
			FieldInfo[] fields = c.GetType().GetFields();
			foreach (FieldInfo fieldInfo in fields)
			{
				if (VolumeEffectField.IsValidType(fieldInfo.FieldType.FullName))
				{
					componentFields.Add(new VolumeEffectFieldFlags(fieldInfo));
				}
			}
		}

		public void UpdateComponentFlags(VolumeEffectComponent comp)
		{
			using (List<VolumeEffectField>.Enumerator enumerator = comp.fields.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					_003C_003Ec__DisplayClass6_0 _003C_003Ec__DisplayClass6_ = new _003C_003Ec__DisplayClass6_0();
					_003C_003Ec__DisplayClass6_.field = enumerator.Current;
					if (componentFields.Find(_003C_003Ec__DisplayClass6_._003CUpdateComponentFlags_003Eb__0) == null && VolumeEffectField.IsValidType(_003C_003Ec__DisplayClass6_.field.fieldType))
					{
						componentFields.Add(new VolumeEffectFieldFlags(_003C_003Ec__DisplayClass6_.field));
					}
				}
			}
		}

		public void UpdateComponentFlags(Component c)
		{
			FieldInfo[] fields = c.GetType().GetFields();
			for (int i = 0; i < fields.Length; i++)
			{
				_003C_003Ec__DisplayClass7_0 _003C_003Ec__DisplayClass7_ = new _003C_003Ec__DisplayClass7_0();
				_003C_003Ec__DisplayClass7_.pi = fields[i];
				if (!componentFields.Exists(_003C_003Ec__DisplayClass7_._003CUpdateComponentFlags_003Eb__0) && VolumeEffectField.IsValidType(_003C_003Ec__DisplayClass7_.pi.FieldType.FullName))
				{
					componentFields.Add(new VolumeEffectFieldFlags(_003C_003Ec__DisplayClass7_.pi));
				}
			}
		}

		public string[] GetFieldNames()
		{
			return componentFields.Where(_003C_003Ec._003C_003E9__8_0 ?? (_003C_003Ec._003C_003E9__8_0 = _003C_003Ec._003C_003E9._003CGetFieldNames_003Eb__8_0)).Select(_003C_003Ec._003C_003E9__8_1 ?? (_003C_003Ec._003C_003E9__8_1 = _003C_003Ec._003C_003E9._003CGetFieldNames_003Eb__8_1)).ToArray();
		}
	}
}
