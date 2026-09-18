using System;
using UnityEngine;

namespace emotitron.Utilities
{
	[Serializable]
	public struct MarkerNameType
	{
		[HideInInspector]
		public MarkerType type;

		[HideInInspector]
		public int hash;

		[HideInInspector]
		public string name;

		public static string[] enumNames = Enum.GetNames(typeof(MarkerType));

		public MarkerNameType(MarkerType vitalType)
		{
			type = vitalType;
			name = Enum.GetName(typeof(MarkerType), vitalType);
			hash = name.GetHashCode();
		}

		public MarkerNameType(string name)
		{
			type = (MarkerType)NameTypeUtils.GetVitalTypeForName(name, enumNames);
			this.name = name;
			hash = name.GetHashCode();
		}

		public override string ToString()
		{
			return string.Concat("NameType: ", type, " ", name, " ", hash);
		}
	}
}
