using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Photon.Utilities
{
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
			SmartVar result = default(SmartVar);
			result.Int = v;
			result.TypeCode = SmartVarTypeCode.Int;
			return result;
		}

		public static implicit operator SmartVar(uint v)
		{
			SmartVar result = default(SmartVar);
			result.UInt = v;
			result.TypeCode = SmartVarTypeCode.Uint;
			return result;
		}

		public static implicit operator SmartVar(float v)
		{
			SmartVar result = default(SmartVar);
			result.Float = v;
			result.TypeCode = SmartVarTypeCode.Float;
			return result;
		}

		public static implicit operator SmartVar(bool v)
		{
			SmartVar result = default(SmartVar);
			result.Bool = v;
			result.TypeCode = SmartVarTypeCode.Bool;
			return result;
		}

		public static implicit operator SmartVar(byte v)
		{
			SmartVar result = default(SmartVar);
			result.Byte8 = v;
			result.TypeCode = SmartVarTypeCode.Byte;
			return result;
		}

		public static implicit operator SmartVar(short v)
		{
			SmartVar result = default(SmartVar);
			result.Short = v;
			result.TypeCode = SmartVarTypeCode.Short;
			return result;
		}

		public static implicit operator SmartVar(ushort v)
		{
			SmartVar result = default(SmartVar);
			result.UShort = v;
			result.TypeCode = SmartVarTypeCode.UShort;
			return result;
		}

		public static implicit operator SmartVar(char v)
		{
			SmartVar result = default(SmartVar);
			result.Char = v;
			result.TypeCode = SmartVarTypeCode.Char;
			return result;
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
			SmartVar result = default(SmartVar);
			result.TypeCode = TypeCode;
			result.Int = Int;
			return result;
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
}
