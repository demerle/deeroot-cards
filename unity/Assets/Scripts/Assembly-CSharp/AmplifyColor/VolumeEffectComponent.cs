using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace AmplifyColor
{
	[Serializable]
	public class VolumeEffectComponent
	{
		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass7_0
		{
			public VolumeEffectFieldFlags fieldFlags;

			internal bool _003CUpdateComponent_003Eb__0(VolumeEffectField s)
			{
				return s.fieldName == fieldFlags.fieldName;
			}
		}

		[Serializable]
		[CompilerGenerated]
		private sealed class _003C_003Ec
		{
			public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

			public static Func<FieldInfo, bool> _003C_003E9__9_0;

			public static Func<VolumeEffectField, string> _003C_003E9__10_0;

			internal bool _003CListAcceptableFields_003Eb__9_0(FieldInfo f)
			{
				return VolumeEffectField.IsValidType(f.FieldType.FullName);
			}

			internal string _003CGetFieldNames_003Eb__10_0(VolumeEffectField r)
			{
				return r.fieldName;
			}
		}

		public string componentName;

		public List<VolumeEffectField> fields;

		public VolumeEffectComponent(string name)
		{
			componentName = name;
			fields = new List<VolumeEffectField>();
		}

		public VolumeEffectField AddField(FieldInfo pi, Component c)
		{
			return AddField(pi, c, -1);
		}

		public VolumeEffectField AddField(FieldInfo pi, Component c, int position)
		{
			VolumeEffectField volumeEffectField = (VolumeEffectField.IsValidType(pi.FieldType.FullName) ? new VolumeEffectField(pi, c) : null);
			if (volumeEffectField != null)
			{
				if (position < 0 || position >= fields.Count)
				{
					fields.Add(volumeEffectField);
				}
				else
				{
					fields.Insert(position, volumeEffectField);
				}
			}
			return volumeEffectField;
		}

		public void RemoveEffectField(VolumeEffectField field)
		{
			fields.Remove(field);
		}

		public VolumeEffectComponent(Component c, VolumeEffectComponentFlags compFlags)
			: this(compFlags.componentName)
		{
			foreach (VolumeEffectFieldFlags componentField in compFlags.componentFields)
			{
				if (componentField.blendFlag)
				{
					FieldInfo field = c.GetType().GetField(componentField.fieldName);
					VolumeEffectField volumeEffectField = (VolumeEffectField.IsValidType(field.FieldType.FullName) ? new VolumeEffectField(field, c) : null);
					if (volumeEffectField != null)
					{
						fields.Add(volumeEffectField);
					}
				}
			}
		}

		public void UpdateComponent(Component c, VolumeEffectComponentFlags compFlags)
		{
			using (List<VolumeEffectFieldFlags>.Enumerator enumerator = compFlags.componentFields.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					_003C_003Ec__DisplayClass7_0 _003C_003Ec__DisplayClass7_ = new _003C_003Ec__DisplayClass7_0();
					_003C_003Ec__DisplayClass7_.fieldFlags = enumerator.Current;
					if (_003C_003Ec__DisplayClass7_.fieldFlags.blendFlag && !fields.Exists(_003C_003Ec__DisplayClass7_._003CUpdateComponent_003Eb__0))
					{
						FieldInfo field = c.GetType().GetField(_003C_003Ec__DisplayClass7_.fieldFlags.fieldName);
						VolumeEffectField volumeEffectField = (VolumeEffectField.IsValidType(field.FieldType.FullName) ? new VolumeEffectField(field, c) : null);
						if (volumeEffectField != null)
						{
							fields.Add(volumeEffectField);
						}
					}
				}
			}
		}

		public VolumeEffectField FindEffectField(string fieldName)
		{
			for (int i = 0; i < fields.Count; i++)
			{
				if (fields[i].fieldName == fieldName)
				{
					return fields[i];
				}
			}
			return null;
		}

		public static FieldInfo[] ListAcceptableFields(Component c)
		{
			if (c == null)
			{
				return new FieldInfo[0];
			}
			return c.GetType().GetFields().Where(_003C_003Ec._003C_003E9__9_0 ?? (_003C_003Ec._003C_003E9__9_0 = _003C_003Ec._003C_003E9._003CListAcceptableFields_003Eb__9_0))
				.ToArray();
		}

		public string[] GetFieldNames()
		{
			return fields.Select(_003C_003Ec._003C_003E9__10_0 ?? (_003C_003Ec._003C_003E9__10_0 = _003C_003Ec._003C_003E9._003CGetFieldNames_003Eb__10_0)).ToArray();
		}
	}
}
