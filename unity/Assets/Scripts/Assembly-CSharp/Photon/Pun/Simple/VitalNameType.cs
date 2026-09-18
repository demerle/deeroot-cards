using System;
using UnityEngine;
using emotitron.Utilities;

namespace Photon.Pun.Simple
{
	[Serializable]
	public struct VitalNameType
	{
		[HideInInspector]
		public VitalType type;

		[HideInInspector]
		public int hash;

		[HideInInspector]
		public string name;

		public static string[] enumNames = Enum.GetNames(typeof(VitalType));

		public string[] EnumNames
		{
			get
			{
				return enumNames;
			}
		}

		public VitalNameType(VitalType vitalType)
		{
			type = vitalType;
			name = Enum.GetName(typeof(VitalType), vitalType);
			hash = name.GetHashCode();
		}

		public VitalNameType(string name)
		{
			type = (VitalType)NameTypeUtils.GetVitalTypeForName(name, enumNames);
			this.name = name;
			hash = name.GetHashCode();
		}

		public override string ToString()
		{
			return string.Concat("VitalNameType: ", type, " ", name, " ", hash);
		}
	}
}
