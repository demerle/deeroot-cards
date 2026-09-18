using System;
using Photon.Compression.Internal;
using Photon.Utilities;

namespace Photon.Compression
{
	public class SyncRangedIntAttribute : SyncVarBaseAttribute, IPackByte, IPackSByte, IPackUInt16, IPackInt16, IPackUInt32, IPackInt32, IPackUInt64, IPackInt64, IPackSingle, IPackDouble
	{
		private int min;

		private int max;

		private readonly int smallest;

		private readonly int biggest;

		private readonly IndicatorBits indicatorBits;

		public SyncRangedIntAttribute(int min, int max, IndicatorBits indicatorBits = IndicatorBits.None, KeyRate keyRate = KeyRate.UseDefault)
		{
			this.min = min;
			this.max = max;
			this.indicatorBits = indicatorBits;
			base.keyRate = keyRate;
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
			uint maxvalue = (uint)(biggest - smallest);
			bitCount = maxvalue.GetBitsForMaxValue();
		}

		public override int GetMaxBits(Type fieldType)
		{
			switch (indicatorBits)
			{
			case IndicatorBits.IsZero:
				return bitCount + 1;
			case IndicatorBits.IsZeroMidMinMax:
				return bitCount + 2;
			default:
				return bitCount;
			}
		}

		private SerializationFlags Write(int value, int prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			int num = ((value > biggest) ? biggest : ((value < smallest) ? smallest : value));
			if (!IsForced(frameId, num, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			if (indicatorBits == IndicatorBits.IsZero)
			{
				if (num == 0)
				{
					buffer.Write(0uL, ref bitposition, 1);
					return SerializationFlags.IsComplete;
				}
				buffer.Write(1uL, ref bitposition, 1);
			}
			else if (indicatorBits == IndicatorBits.IsZeroMidMinMax)
			{
				if (num == 0)
				{
					buffer.Write(0uL, ref bitposition, 1);
					return SerializationFlags.IsComplete;
				}
				if (num == min)
				{
					buffer.Write(1uL, ref bitposition, 1);
					return SerializationFlags.IsComplete;
				}
				if (num == max)
				{
					buffer.Write(3uL, ref bitposition, 1);
					return SerializationFlags.IsComplete;
				}
				buffer.Write(3uL, ref bitposition, 1);
			}
			if (bitCount < 16)
			{
				buffer.Write((ulong)(num - smallest), ref bitposition, bitCount);
			}
			else
			{
				buffer.WritePackedBytes((ulong)(num - smallest), ref bitposition, bitCount);
			}
			return SerializationFlags.IsComplete;
		}

		private int Read(byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (indicatorBits == IndicatorBits.IsZero)
			{
				if (buffer.Read(ref bitposition, 1) == 0L)
				{
					return 0;
				}
			}
			else if (indicatorBits == IndicatorBits.IsZeroMidMinMax)
			{
				ulong num = buffer.Read(ref bitposition, 2);
				if (num <= 2)
				{
					switch (num)
					{
					case 0uL:
						return 0;
					case 1uL:
						return min;
					case 2uL:
						return max;
					}
				}
			}
			if (bitCount < 16)
			{
				return (int)buffer.Read(ref bitposition, bitCount) + smallest;
			}
			return (int)((long)buffer.ReadPackedBytes(ref bitposition, bitCount) + (long)smallest);
		}

		public SerializationFlags Pack(ref byte value, byte prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			return Write(value, prevValue, buffer, ref bitposition, frameId, writeFlags);
		}

		public SerializationFlags Unpack(ref byte value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = (byte)Read(buffer, ref bitposition, frameId, writeFlags);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref sbyte value, sbyte prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			return Write(value, prevValue, buffer, ref bitposition, frameId, writeFlags);
		}

		public SerializationFlags Unpack(ref sbyte value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = (sbyte)Read(buffer, ref bitposition, frameId, writeFlags);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref ushort value, ushort prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			return Write(value, prevValue, buffer, ref bitposition, frameId, writeFlags);
		}

		public SerializationFlags Unpack(ref ushort value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = (ushort)Read(buffer, ref bitposition, frameId, writeFlags);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref short value, short prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			return Write(value, prevValue, buffer, ref bitposition, frameId, writeFlags);
		}

		public SerializationFlags Unpack(ref short value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = (short)Read(buffer, ref bitposition, frameId, writeFlags);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref uint value, uint prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			return Write((int)value, (int)prevValue, buffer, ref bitposition, frameId, writeFlags);
		}

		public SerializationFlags Unpack(ref uint value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = (uint)Read(buffer, ref bitposition, frameId, writeFlags);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref int value, int prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			return Write(value, prevValue, buffer, ref bitposition, frameId, writeFlags);
		}

		public SerializationFlags Unpack(ref int value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = Read(buffer, ref bitposition, frameId, writeFlags);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref ulong value, ulong prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			return Write((int)value, (int)prevValue, buffer, ref bitposition, frameId, writeFlags);
		}

		public SerializationFlags Unpack(ref ulong value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = (ulong)Read(buffer, ref bitposition, frameId, writeFlags);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref long value, long prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			return Write((int)value, (int)prevValue, buffer, ref bitposition, frameId, writeFlags);
		}

		public SerializationFlags Unpack(ref long value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = Read(buffer, ref bitposition, frameId, writeFlags);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref float value, float prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			int num = (int)Math.Round(value);
			int num2 = (int)Math.Round(prevValue);
			if (!IsForced(frameId, writeFlags) && num == num2)
			{
				return SerializationFlags.None;
			}
			return Write(num, num2, buffer, ref bitposition, frameId, writeFlags);
		}

		public SerializationFlags Unpack(ref float value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = Read(buffer, ref bitposition, frameId, writeFlags);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref double value, double prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			int num = (int)Math.Round(value);
			int num2 = (int)Math.Round(prevValue);
			if (!IsForced(frameId, writeFlags) && num == num2)
			{
				return SerializationFlags.None;
			}
			return Write(num, num2, buffer, ref bitposition, frameId, writeFlags);
		}

		public SerializationFlags Unpack(ref double value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = Read(buffer, ref bitposition, frameId, writeFlags);
			return SerializationFlags.IsComplete;
		}
	}
}
