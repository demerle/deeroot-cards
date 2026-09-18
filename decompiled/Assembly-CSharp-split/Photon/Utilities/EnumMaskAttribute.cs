using System;
using UnityEngine;

namespace Photon.Utilities;

[AttributeUsage(AttributeTargets.Field)]
public class EnumMaskAttribute : PropertyAttribute
{
	public bool definesZero;

	public Type castTo;

	public EnumMaskAttribute(bool definesZero = false, Type castTo = null)
	{
		this.castTo = castTo;
		this.definesZero = definesZero;
	}
}
