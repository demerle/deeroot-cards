using UnityEngine;

namespace Photon.Utilities
{
	public class BitsPerRangeAttribute : PropertyAttribute
	{
		public readonly int max;

		public readonly int min;

		public readonly string label;

		public readonly bool showLabel;

		public readonly string tooltip;

		public bool show;

		public BitsPerRangeAttribute(int min, int max, bool show, bool zeroBase = false, string label = "Max:", bool showLabel = true, string tooltip = "")
		{
			this.show = show;
			this.min = min;
			this.max = max;
			this.label = label;
			this.showLabel = showLabel;
			this.tooltip = tooltip;
		}
	}
}
