using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Photon.Compression;

[StructLayout(LayoutKind.Explicit)]
public struct Element : IEquatable<Element>
{
	public enum VectorType
	{
		Vector3 = 1,
		Quaternion
	}

	[FieldOffset(0)]
	public VectorType vectorType;

	[FieldOffset(4)]
	public Vector3 v;

	[FieldOffset(4)]
	public Quaternion quat;

	public Element(Vector3 v)
	{
		this = default(Element);
		vectorType = VectorType.Vector3;
		this.v = v;
	}

	public Element(Quaternion quat)
	{
		this = default(Element);
		vectorType = VectorType.Quaternion;
		this.quat = quat;
	}

	public static explicit operator Quaternion(Element e)
	{
		if (e.vectorType == VectorType.Quaternion)
		{
			return e.quat;
		}
		return Quaternion.Euler(e.v);
	}

	public static explicit operator Vector3(Element e)
	{
		if (e.vectorType == VectorType.Vector3)
		{
			return e.v;
		}
		return e.quat.eulerAngles;
	}

	public static Element Slerp(Element a, Element b, float t)
	{
		if (a.vectorType == VectorType.Quaternion)
		{
			return Quaternion.Slerp((Quaternion)a, (Quaternion)b, t);
		}
		return Vector3.Slerp((Vector3)a, (Vector3)b, t);
	}

	public static Element SlerpUnclamped(Element a, Element b, float t)
	{
		if (a.vectorType == VectorType.Quaternion)
		{
			return Quaternion.SlerpUnclamped((Quaternion)a, (Quaternion)b, t);
		}
		return Vector3.SlerpUnclamped((Vector3)a, (Vector3)b, t);
	}

	public static bool operator ==(Element a, Element b)
	{
		if (a.vectorType != b.vectorType || a.vectorType != VectorType.Vector3)
		{
			if (a.quat.x == b.quat.x && a.quat.y == b.quat.y && a.quat.z == b.quat.z)
			{
				return a.quat.w == b.quat.w;
			}
			return false;
		}
		if (a.v.x == b.v.x && a.v.y == b.v.y)
		{
			return a.v.z == b.v.z;
		}
		return false;
	}

	public static bool operator !=(Element a, Element b)
	{
		return !(a == b);
	}

	public override bool Equals(object obj)
	{
		if (obj is Element)
		{
			return Equals((Element)obj);
		}
		return false;
	}

	public bool Equals(Element other)
	{
		if (vectorType != other.vectorType || vectorType != VectorType.Vector3)
		{
			if (quat.x == other.quat.x && quat.y == other.quat.y && quat.z == other.quat.z)
			{
				return quat.w == other.quat.w;
			}
			return false;
		}
		if (v.x == other.v.x && v.y == other.v.y)
		{
			return v.z == other.v.z;
		}
		return false;
	}

	public static bool Equals(Vector3 a, Vector3 b)
	{
		if (a.x == b.x && a.y == b.y)
		{
			return a.z == b.z;
		}
		return false;
	}

	public static bool Equals(Quaternion a, Quaternion b)
	{
		if (a.x == b.x && a.y == b.y && a.z == b.z)
		{
			return a.w == b.w;
		}
		return false;
	}

	public static implicit operator Element(Quaternion q)
	{
		return new Element(q);
	}

	public static implicit operator Element(Vector3 v)
	{
		return new Element(v);
	}

	public override string ToString()
	{
		return string.Concat(vectorType, " ", (vectorType == VectorType.Quaternion) ? quat.ToString() : v.ToString());
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}
}
