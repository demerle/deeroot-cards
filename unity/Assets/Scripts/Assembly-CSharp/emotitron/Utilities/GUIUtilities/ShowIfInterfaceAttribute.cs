using System;
using UnityEngine;

namespace emotitron.Utilities.GUIUtilities
{
	public class ShowIfInterfaceAttribute : PropertyAttribute
	{
		public readonly Type type;

		public readonly string tooltip;

		public readonly float min;

		public readonly float max;

		public ShowIfInterfaceAttribute(Type type, string tooltip)
		{
			this.type = type;
			this.tooltip = tooltip;
		}

		public ShowIfInterfaceAttribute(Type type, string tooltip, float min, float max)
		{
			this.type = type;
			this.tooltip = tooltip;
			this.min = min;
			this.max = max;
		}
	}
}
