using System;
using UnityEngine;

namespace emotitron.Utilities.GUIUtilities
{
	[AttributeUsage(AttributeTargets.Field)]
	public class ValueTypeAttribute : PropertyAttribute
	{
		public string labeltag;

		public float width;

		public ValueTypeAttribute(string labeltag, float width = 48f)
		{
			this.labeltag = labeltag;
			this.width = width;
		}
	}
}
