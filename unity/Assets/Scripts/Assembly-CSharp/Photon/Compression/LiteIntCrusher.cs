using System;
using UnityEngine;

namespace Photon.Compression
{
	[Serializable]
	public class LiteIntCrusher : LiteCrusher<uint, int>
	{
		[SerializeField]
		public LiteIntCompressType compressType;

		[SerializeField]
		protected int min;

		[SerializeField]
		protected int max;

		[SerializeField]
		private int smallest;

		[SerializeField]
		private int biggest;

		public LiteIntCrusher()
		{
			compressType = LiteIntCompressType.PackSigned;
			min = -128;
			max = 127;
			if (compressType == LiteIntCompressType.Range)
			{
				Recalculate(min, max, ref smallest, ref biggest, ref bits);
			}
		}

		public LiteIntCrusher(LiteIntCompressType compType = LiteIntCompressType.PackSigned, int min = -128, int max = 127)
		{
			compressType = compType;
			this.min = min;
			this.max = max;
			if (compressType == LiteIntCompressType.Range)
			{
				Recalculate(min, max, ref smallest, ref biggest, ref bits);
			}
		}

		public override uint WriteValue(int val, byte[] buffer, ref int bitposition)
		{
			switch (compressType)
			{
			case LiteIntCompressType.PackUnsigned:
				buffer.WritePackedBytes((uint)val, ref bitposition, 32);
				return (uint)val;
			case LiteIntCompressType.PackSigned:
			{
				uint num2 = (uint)((val << 1) ^ (val >> 31));
				buffer.WritePackedBytes(num2, ref bitposition, 32);
				return num2;
			}
			case LiteIntCompressType.Range:
			{
				uint num = Encode(val);
				buffer.Write(num, ref bitposition, bits);
				return num;
			}
			default:
				return 0u;
			}
		}

		public override void WriteCValue(uint cval, byte[] buffer, ref int bitposition)
		{
			switch (compressType)
			{
			case LiteIntCompressType.PackUnsigned:
				buffer.WritePackedBytes(cval, ref bitposition, 32);
				break;
			case LiteIntCompressType.PackSigned:
				buffer.WritePackedBytes(cval, ref bitposition, 32);
				break;
			case LiteIntCompressType.Range:
				buffer.Write(cval, ref bitposition, bits);
				break;
			}
		}

		public override int ReadValue(byte[] buffer, ref int bitposition)
		{
			switch (compressType)
			{
			case LiteIntCompressType.PackUnsigned:
				return (int)buffer.ReadPackedBytes(ref bitposition, 32);
			case LiteIntCompressType.PackSigned:
				return buffer.ReadSignedPackedBytes(ref bitposition, 32);
			case LiteIntCompressType.Range:
			{
				uint val = (uint)buffer.Read(ref bitposition, bits);
				return Decode(val);
			}
			default:
				return 0;
			}
		}

		public override uint ReadCValue(byte[] buffer, ref int bitposition)
		{
			switch (compressType)
			{
			case LiteIntCompressType.PackUnsigned:
				return (uint)buffer.ReadPackedBytes(ref bitposition, 32);
			case LiteIntCompressType.PackSigned:
				return (uint)buffer.ReadPackedBytes(ref bitposition, 32);
			case LiteIntCompressType.Range:
				return (uint)buffer.Read(ref bitposition, bits);
			default:
				return 0u;
			}
		}

		public override uint Encode(int value)
		{
			switch (compressType)
			{
			case LiteIntCompressType.PackSigned:
				return value.ZigZag();
			case LiteIntCompressType.PackUnsigned:
				return (uint)value;
			default:
				value = ((value > biggest) ? biggest : ((value < smallest) ? smallest : value));
				return (uint)(value - smallest);
			}
		}

		public override int Decode(uint cvalue)
		{
			switch (compressType)
			{
			case LiteIntCompressType.PackSigned:
				return cvalue.UnZigZag();
			case LiteIntCompressType.PackUnsigned:
				return (int)cvalue;
			default:
				return (int)(cvalue + smallest);
			}
		}

		public static void Recalculate(int min, int max, LiteIntCrusher crusher)
		{
			if (min < max)
			{
				crusher.smallest = min;
				crusher.biggest = max;
			}
			else
			{
				crusher.smallest = max;
				crusher.biggest = min;
			}
			int maxvalue = crusher.biggest - crusher.smallest;
			crusher.bits = LiteCrusher.GetBitsForMaxValue((uint)maxvalue);
		}

		public static void Recalculate(int min, int max, ref int smallest, ref int biggest, ref int bits)
		{
			if (min < max)
			{
				smallest = min;
				biggest = max;
			}
			else
			{
				smallest = max;
				biggest = min;
			}
			int maxvalue = biggest - smallest;
			bits = LiteCrusher.GetBitsForMaxValue((uint)maxvalue);
		}
	}
}
