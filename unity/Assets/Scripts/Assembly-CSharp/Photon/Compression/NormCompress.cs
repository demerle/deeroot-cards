using Photon.Compression.HalfFloat;

namespace Photon.Compression
{
	public static class NormCompress
	{
		public struct NormCompressCodec
		{
			public readonly int bits;

			public readonly float encoder;

			public readonly float decoder;

			public NormCompressCodec(int bits, float encoder, float decoder)
			{
				this.bits = bits;
				this.encoder = encoder;
				this.decoder = decoder;
			}
		}

		public static NormCompressCodec[] codecForBit;

		private const float NORM_COMP_ENCODE15 = 32767f;

		private const float NORM_COMP_DECODE15 = 3.051851E-05f;

		private const float NORM_COMP_ENCODE14 = 16383f;

		private const float NORM_COMP_DECODE14 = 6.103888E-05f;

		private const float NORM_COMP_ENCODE13 = 8191f;

		private const float NORM_COMP_DECODE13 = 0.00012208521f;

		private const float NORM_COMP_ENCODE12 = 4095f;

		private const float NORM_COMP_DECODE12 = 0.00024420026f;

		private const float NORM_COMP_ENCODE11 = 2047f;

		private const float NORM_COMP_DECODE11 = 0.0004885198f;

		private const float NORM_COMP_ENCODE10 = 1023f;

		private const float NORM_COMP_DECODE10 = 0.0009775171f;

		private const float NORM_COMP_ENCODE9 = 511f;

		private const float NORM_COMP_DECODE9 = 0.0019569471f;

		private const float NORM_COMP_ENCODE8 = 255f;

		private const float NORM_COMP_DECODE8 = 0.003921569f;

		private const float NORM_COMP_ENCODE7 = 127f;

		private const float NORM_COMP_DECODE7 = 0.003921569f;

		private const float NORM_COMP_ENCODE6 = 63f;

		private const float NORM_COMP_DECODE6 = 1f / 63f;

		private const float NORM_COMP_ENCODE5 = 31f;

		private const float NORM_COMP_DECODE5 = 1f / 31f;

		private const float NORM_COMP_ENCODE4 = 15f;

		private const float NORM_COMP_DECODE4 = 1f / 15f;

		private const float NORM_COMP_ENCODE3 = 7f;

		private const float NORM_COMP_DECODE3 = 1f / 7f;

		private const float NORM_COMP_ENCODE2 = 3f;

		private const float NORM_COMP_DECODE2 = 1f / 3f;

		private const float NORM_COMP_ENCODE1 = 1f;

		private const float NORM_COMP_DECODE1 = 1f;

		private const float NORM_COMP_ENCODE0 = 0f;

		private const float NORM_COMP_DECODE0 = 0f;

		static NormCompress()
		{
			codecForBit = new NormCompressCodec[33];
			for (int i = 0; i <= 32; i++)
			{
				uint maxValueForBits = GetMaxValueForBits(i);
				codecForBit[i] = new NormCompressCodec(i, maxValueForBits, 1f / (float)maxValueForBits);
			}
		}

		public static uint CompressNorm(this float value, int bits)
		{
			value = ((value > 1f) ? 1f : ((value < 0f) ? 0f : value));
			switch (bits)
			{
			case 0:
				return 0u;
			case 1:
				return (uint)value;
			case 2:
				return (uint)(value * 3f);
			case 3:
				return (uint)(value * 7f);
			case 4:
				return (uint)(value * 15f);
			case 5:
				return (uint)(value * 31f);
			case 6:
				return (uint)(value * 63f);
			case 7:
				return (uint)(value * 127f);
			case 8:
				return (uint)(value * 255f);
			case 9:
				return (uint)(value * 511f);
			case 10:
				return (uint)(value * 1023f);
			case 11:
				return (uint)(value * 2047f);
			case 12:
				return (uint)(value * 4095f);
			case 13:
				return (uint)(value * 8191f);
			case 14:
				return (uint)(value * 16383f);
			case 15:
				return (uint)(value * 32767f);
			case 16:
				return HalfUtilities.Pack(value);
			default:
				return (ByteConverter)value;
			}
		}

		public static uint WriteNorm(this byte[] buffer, float value, ref int bitposition, int bits)
		{
			value = ((value > 1f) ? 1f : ((value < 0f) ? 0f : value));
			uint num;
			switch (bits)
			{
			case 0:
				num = 0u;
				break;
			case 1:
				num = (uint)value;
				break;
			case 2:
				num = (uint)(value * 3f);
				break;
			case 3:
				num = (uint)(value * 7f);
				break;
			case 4:
				num = (uint)(value * 15f);
				break;
			case 5:
				num = (uint)(value * 31f);
				break;
			case 6:
				num = (uint)(value * 63f);
				break;
			case 7:
				num = (uint)(value * 127f);
				break;
			case 8:
				num = (uint)(value * 255f);
				break;
			case 9:
				num = (uint)(value * 511f);
				break;
			case 10:
				num = (uint)(value * 1023f);
				break;
			case 11:
				num = (uint)(value * 2047f);
				break;
			case 12:
				num = (uint)(value * 4095f);
				break;
			case 13:
				num = (uint)(value * 8191f);
				break;
			case 14:
				num = (uint)(value * 16383f);
				break;
			case 15:
				num = (uint)(value * 32767f);
				break;
			case 16:
				num = HalfUtilities.Pack(value);
				break;
			default:
				num = (ByteConverter)value;
				break;
			}
			buffer.Write(num, ref bitposition, bits);
			return num;
		}

		public static float ReadNorm(this byte[] buffer, ref int bitposition, int bits)
		{
			switch (bits)
			{
			case 0:
				return 0f;
			case 1:
				return (float)buffer.Read(ref bitposition, 1) * 1f;
			case 2:
				return (float)buffer.Read(ref bitposition, 2) * (1f / 3f);
			case 3:
				return (float)buffer.Read(ref bitposition, 3) * (1f / 7f);
			case 4:
				return (float)buffer.Read(ref bitposition, 4) * (1f / 15f);
			case 5:
				return (float)buffer.Read(ref bitposition, 5) * (1f / 31f);
			case 6:
				return (float)buffer.Read(ref bitposition, 6) * (1f / 63f);
			case 7:
				return (float)buffer.Read(ref bitposition, 7) * 0.003921569f;
			case 8:
				return (float)buffer.Read(ref bitposition, 8) * 0.003921569f;
			case 9:
				return (float)buffer.Read(ref bitposition, 9) * 0.0019569471f;
			case 10:
				return (float)buffer.Read(ref bitposition, 10) * 0.0009775171f;
			case 11:
				return (float)buffer.Read(ref bitposition, 11) * 0.0004885198f;
			case 12:
				return (float)buffer.Read(ref bitposition, 12) * 0.00024420026f;
			case 13:
				return (float)buffer.Read(ref bitposition, 13) * 0.00012208521f;
			case 14:
				return (float)buffer.Read(ref bitposition, 14) * 6.103888E-05f;
			case 15:
				return (float)buffer.Read(ref bitposition, 15) * 3.051851E-05f;
			case 16:
				return buffer.ReadHalf(ref bitposition);
			default:
				return buffer.ReadFloat(ref bitposition);
			}
		}

		public static uint GetMaxValueForBits(int bitcount)
		{
			return (uint)((1L << bitcount) - 1);
		}
	}
}
