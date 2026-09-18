using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Photon.Utilities;

[Serializable]
[StructLayout(LayoutKind.Explicit)]
public struct SmartVar
{
	[FieldOffset(0)]
	public SmartVarTypeCode TypeCode;

	[FieldOffset(4)]
	public int Int;

	[FieldOffset(4)]
	public uint UInt;

	[FieldOffset(4)]
	public bool Bool;

	[FieldOffset(4)]
	public float Float;

	[FieldOffset(4)]
	public byte Byte8;

	[FieldOffset(4)]
	public short Short;

	[FieldOffset(4)]
	public ushort UShort;

	[FieldOffset(4)]
	public char Char;

	public static readonly SmartVar None = new SmartVar
	{
		TypeCode = SmartVarTypeCode.None
	};

	public static implicit operator SmartVar(int v)
	{
		return new SmartVar
		{
			Int = v,
			TypeCode = SmartVarTypeCode.Int
		};
	}

	public static implicit operator SmartVar(uint v)
	{
		return new SmartVar
		{
			UInt = v,
			TypeCode = SmartVarTypeCode.Uint
		};
	}

	public static implicit operator SmartVar(float v)
	{
		return new SmartVar
		{
			Float = v,
			TypeCode = SmartVarTypeCode.Float
		};
	}

	public static implicit operator SmartVar(bool v)
	{
		return new SmartVar
		{
			Bool = v,
			TypeCode = SmartVarTypeCode.Bool
		};
	}

	public static implicit operator SmartVar(byte v)
	{
		return new SmartVar
		{
			Byte8 = v,
			TypeCode = SmartVarTypeCode.Byte
		};
	}

	public static implicit operator SmartVar(short v)
	{
		return new SmartVar
		{
			Short = v,
			TypeCode = SmartVarTypeCode.Short
		};
	}

	public static implicit operator SmartVar(ushort v)
	{
		return new SmartVar
		{
			UShort = v,
			TypeCode = SmartVarTypeCode.UShort
		};
	}

	public static implicit operator SmartVar(char v)
	{
		return new SmartVar
		{
			Char = v,
			TypeCode = SmartVarTypeCode.Char
		};
	}

	public static implicit operator int(SmartVar v)
	{
		if (v.TypeCode == SmartVarTypeCode.Int)
		{
			return v.Int;
		}
		UnityEngine.Debug.Log(v.TypeCode);
		throw new InvalidCastException();
	}

	public static implicit operator uint(SmartVar v)
	{
		if (v.TypeCode == SmartVarTypeCode.Uint)
		{
			return v.UInt;
		}
		throw new InvalidCastException();
	}

	public static implicit operator float(SmartVar v)
	{
		if (v.TypeCode == SmartVarTypeCode.Float)
		{
			return v.Float;
		}
		UnityEngine.Debug.LogError(string.Concat("cant cast ", v.TypeCode, " to single float"));
		throw new InvalidCastException();
	}

	public static implicit operator bool(SmartVar v)
	{
		if (v.TypeCode == SmartVarTypeCode.Bool)
		{
			return v.Bool;
		}
		throw new InvalidCastException();
	}

	public static implicit operator byte(SmartVar v)
	{
		if (v.TypeCode == SmartVarTypeCode.Byte)
		{
			return v.Byte8;
		}
		throw new InvalidCastException();
	}

	public static implicit operator short(SmartVar v)
	{
		if (v.TypeCode == SmartVarTypeCode.Short)
		{
			return v.Short;
		}
		throw new InvalidCastException();
	}

	public static implicit operator ushort(SmartVar v)
	{
		if (v.TypeCode == SmartVarTypeCode.UShort)
		{
			return v.UShort;
		}
		throw new InvalidCastException();
	}

	public static implicit operator char(SmartVar v)
	{
		if (v.TypeCode == SmartVarTypeCode.Char)
		{
			return v.Char;
		}
		throw new InvalidCastException();
	}

	public SmartVar Copy()
	{
		return new SmartVar
		{
			TypeCode = TypeCode,
			Int = Int
		};
	}

	public string ToStringVerbose()
	{
		string text = TypeCode.ToString() + " ";
		if (TypeCode == SmartVarTypeCode.None)
		{
			return text;
		}
		if (TypeCode == SmartVarTypeCode.Bool)
		{
			return text + Bool;
		}
		if (TypeCode == SmartVarTypeCode.Int)
		{
			return text + Int;
		}
		if (TypeCode == SmartVarTypeCode.Uint)
		{
			return text + UInt;
		}
		if (TypeCode == SmartVarTypeCode.Float)
		{
			return text + Float;
		}
		if (TypeCode == SmartVarTypeCode.Short)
		{
			return text + Short;
		}
		if (TypeCode == SmartVarTypeCode.UShort)
		{
			return text + UShort;
		}
		if (TypeCode == SmartVarTypeCode.Byte)
		{
			return text + Byte8;
		}
		if (TypeCode == SmartVarTypeCode.Char)
		{
			return text + Char;
		}
		return text;
	}

	public override string ToString()
	{
		if (TypeCode == SmartVarTypeCode.None)
		{
			return "";
		}
		if (TypeCode == SmartVarTypeCode.Bool)
		{
			return Bool.ToString();
		}
		if (TypeCode == SmartVarTypeCode.Int)
		{
			return Int.ToString();
		}
		if (TypeCode == SmartVarTypeCode.Uint)
		{
			return UInt.ToString();
		}
		if (TypeCode == SmartVarTypeCode.Float)
		{
			return Float.ToString();
		}
		if (TypeCode == SmartVarTypeCode.Short)
		{
			return Short.ToString();
		}
		if (TypeCode == SmartVarTypeCode.UShort)
		{
			return UShort.ToString();
		}
		if (TypeCode == SmartVarTypeCode.Byte)
		{
			return Byte8.ToString();
		}
		if (TypeCode == SmartVarTypeCode.Char)
		{
			return Char.ToString();
		}
		return "";
	}
}
