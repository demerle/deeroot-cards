using Photon.Compression.HalfFloat;

namespace Photon.Compression;

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
		return bits switch
		{
			0 => 0u, 
			1 => (uint)value, 
			2 => (uint)(value * 3f), 
			3 => (uint)(value * 7f), 
			4 => (uint)(value * 15f), 
			5 => (uint)(value * 31f), 
			6 => (uint)(value * 63f), 
			7 => (uint)(value * 127f), 
			8 => (uint)(value * 255f), 
			9 => (uint)(value * 511f), 
			10 => (uint)(value * 1023f), 
			11 => (uint)(value * 2047f), 
			12 => (uint)(value * 4095f), 
			13 => (uint)(value * 8191f), 
			14 => (uint)(value * 16383f), 
			15 => (uint)(value * 32767f), 
			16 => HalfUtilities.Pack(value), 
			_ => (ByteConverter)value, 
		};
	}

	public static uint WriteNorm(this byte[] buffer, float value, ref int bitposition, int bits)
	{
		value = ((value > 1f) ? 1f : ((value < 0f) ? 0f : value));
		uint num = bits switch
		{
			0 => 0u, 
			1 => (uint)value, 
			2 => (uint)(value * 3f), 
			3 => (uint)(value * 7f), 
			4 => (uint)(value * 15f), 
			5 => (uint)(value * 31f), 
			6 => (uint)(value * 63f), 
			7 => (uint)(value * 127f), 
			8 => (uint)(value * 255f), 
			9 => (uint)(value * 511f), 
			10 => (uint)(value * 1023f), 
			11 => (uint)(value * 2047f), 
			12 => (uint)(value * 4095f), 
			13 => (uint)(value * 8191f), 
			14 => (uint)(value * 16383f), 
			15 => (uint)(value * 32767f), 
			16 => HalfUtilities.Pack(value), 
			_ => (ByteConverter)value, 
		};
		buffer.Write(num, ref bitposition, bits);
		return num;
	}

	public static float ReadNorm(this byte[] buffer, ref int bitposition, int bits)
	{
		return bits switch
		{
			0 => 0f, 
			1 => (float)buffer.Read(ref bitposition, 1) * 1f, 
			2 => (float)buffer.Read(ref bitposition, 2) * (1f / 3f), 
			3 => (float)buffer.Read(ref bitposition, 3) * (1f / 7f), 
			4 => (float)buffer.Read(ref bitposition, 4) * (1f / 15f), 
			5 => (float)buffer.Read(ref bitposition, 5) * (1f / 31f), 
			6 => (float)buffer.Read(ref bitposition, 6) * (1f / 63f), 
			7 => (float)buffer.Read(ref bitposition, 7) * 0.003921569f, 
			8 => (float)buffer.Read(ref bitposition, 8) * 0.003921569f, 
			9 => (float)buffer.Read(ref bitposition, 9) * 0.0019569471f, 
			10 => (float)buffer.Read(ref bitposition, 10) * 0.0009775171f, 
			11 => (float)buffer.Read(ref bitposition, 11) * 0.0004885198f, 
			12 => (float)buffer.Read(ref bitposition, 12) * 0.00024420026f, 
			13 => (float)buffer.Read(ref bitposition, 13) * 0.00012208521f, 
			14 => (float)buffer.Read(ref bitposition, 14) * 6.103888E-05f, 
			15 => (float)buffer.Read(ref bitposition, 15) * 3.051851E-05f, 
			16 => buffer.ReadHalf(ref bitposition), 
			_ => buffer.ReadFloat(ref bitposition), 
		};
	}

	public static uint GetMaxValueForBits(int bitcount)
	{
		return (uint)((1L << bitcount) - 1);
	}
}
