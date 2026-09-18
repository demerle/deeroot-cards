using System;
using UnityEngine;

namespace Photon.Utilities
{
	public class VersaMaskAttribute : PropertyAttribute
	{
		public bool definesZero;

		public Type castTo;

		public VersaMaskAttribute(bool definesZero = false, Type castTo = null)
		{
			this.definesZero = definesZero;
			this.castTo = castTo;
		}
	}
}
