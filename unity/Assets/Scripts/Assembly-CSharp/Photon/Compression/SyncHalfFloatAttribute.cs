using System;
using Photon.Compression.HalfFloat;
using Photon.Compression.Internal;

namespace Photon.Compression
{
	public class SyncHalfFloatAttribute : SyncVarBaseAttribute, IPackSingle, IPackDouble
	{
		private readonly IndicatorBit indicatorBit;

		public SyncHalfFloatAttribute(IndicatorBit indicatorBit = IndicatorBit.None, KeyRate keyRate = KeyRate.UseDefault)
		{
			this.indicatorBit = indicatorBit;
			base.keyRate = keyRate;
		}

		public override int GetMaxBits(Type fieldType)
		{
			return 16 + ((indicatorBit != 0) ? 1 : 0);
		}

		public SerializationFlags Pack(ref float value, float prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			ushort num = HalfUtilities.Pack(value);
			if (!IsForced(frameId, value, prevValue, writeFlags) && num == HalfUtilities.Pack(prevValue))
			{
				return SerializationFlags.None;
			}
			if (indicatorBit == IndicatorBit.IsZero)
			{
				if (value == 0f)
				{
					buffer.Write(1uL, ref bitposition, 1);
					return SerializationFlags.IsComplete;
				}
				buffer.Write(0uL, ref bitposition, 1);
			}
			buffer.Write(num, ref bitposition, 16);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref float value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (indicatorBit == IndicatorBit.IsZero && buffer.Read(ref bitposition, 1) == 0L)
			{
				value = 0f;
				return SerializationFlags.None;
			}
			ushort value2 = (ushort)buffer.Read(ref bitposition, 16);
			value = HalfUtilities.Unpack(value2);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref double value, double prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			float value2 = (float)value;
			return Pack(ref value2, (float)prevValue, buffer, ref bitposition, frameId, writeFlags);
		}

		public SerializationFlags Unpack(ref double value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			float value2 = 0f;
			SerializationFlags result = Unpack(ref value2, buffer, ref bitposition, frameId, writeFlags);
			value = value2;
			return result;
		}
	}
}
