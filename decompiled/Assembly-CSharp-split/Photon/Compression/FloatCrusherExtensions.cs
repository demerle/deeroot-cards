using System;
using emotitron.Compression;

namespace Photon.Compression;

public static class FloatCrusherExtensions
{
	public static CompressedFloat Write(this FloatCrusher fc, float f, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		CompressedFloat result = fc.Compress(f);
		result.cvalue.Inject(ref buffer, ref bitposition, bits);
		return result;
	}

	public static CompressedFloat Write(this FloatCrusher fc, float f, ref uint buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		CompressedFloat result = fc.Compress(f);
		result.cvalue.Inject(ref buffer, ref bitposition, bits);
		return result;
	}

	public static CompressedFloat Write(this FloatCrusher fc, float f, ref ushort buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		CompressedFloat result = fc.Compress(f);
		result.cvalue.Inject(ref buffer, ref bitposition, bits);
		return result;
	}

	public static CompressedFloat Write(this FloatCrusher fc, float f, ref byte buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		CompressedFloat result = fc.Compress(f);
		result.cvalue.Inject(ref buffer, ref bitposition, bits);
		return result;
	}

	public static CompressedFloat Write(this FloatCrusher fc, CompressedFloat c, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		c.cvalue.Inject(ref buffer, ref bitposition, fc._bits[(int)bcl]);
		return c;
	}

	public static float ReadAndDecompress(this FloatCrusher fc, ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		uint c = (uint)buffer.Read(ref bitposition, bits);
		return fc.Decompress(c);
	}

	public static CompressedFloat Read(this FloatCrusher fc, ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		uint cvalue = (uint)buffer.Read(ref bitposition, bits);
		return new CompressedFloat(fc, cvalue);
	}

	[Obsolete("No reason for buffer to be a ref")]
	public static CompressedFloat Read(this FloatCrusher fc, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int num = fc._bits[(int)bcl];
		uint num2 = fc.masks[(int)bcl];
		uint cvalue = (uint)((buffer >> bitposition) & num2);
		bitposition += num;
		return new CompressedFloat(fc, cvalue);
	}

	public static CompressedFloat Write(this FloatCrusher fc, CompressedFloat c, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		buffer.Write(c.cvalue, ref bitposition, bits);
		return c;
	}

	public static CompressedFloat Write(this FloatCrusher fc, CompressedFloat c, uint[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		buffer.Write(c.cvalue, ref bitposition, bits);
		return c;
	}

	public static CompressedFloat Write(this FloatCrusher fc, CompressedFloat c, ulong[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		buffer.Write(c.cvalue, ref bitposition, bits);
		return c;
	}

	public static CompressedFloat Write(this FloatCrusher fc, uint c, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		buffer.Write(c, ref bitposition, bits);
		return new CompressedFloat(fc, c);
	}

	public static CompressedFloat Write(this FloatCrusher fc, uint c, uint[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		buffer.Write(c, ref bitposition, bits);
		return new CompressedFloat(fc, c);
	}

	public static CompressedFloat Write(this FloatCrusher fc, uint c, ulong[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		buffer.Write(c, ref bitposition, bits);
		return new CompressedFloat(fc, c);
	}

	public static CompressedFloat Write(this FloatCrusher fc, float f, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		uint num = fc.Compress(f);
		int bits = fc._bits[(int)bcl];
		buffer.Write(num, ref bitposition, bits);
		return new CompressedFloat(fc, num);
	}

	public static CompressedFloat Write(this FloatCrusher fc, float f, uint[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		uint num = fc.Compress(f);
		buffer.Write(num, ref bitposition, bits);
		return new CompressedFloat(fc, num);
	}

	public static CompressedFloat Write(this FloatCrusher fc, float f, ulong[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		uint num = fc.Compress(f);
		buffer.Write(num, ref bitposition, bits);
		return new CompressedFloat(fc, num);
	}

	public static CompressedFloat Read(this FloatCrusher fc, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		return new CompressedFloat(fc, buffer.ReadUInt32(ref bitposition, bits));
	}

	public static CompressedFloat Read(this FloatCrusher fc, uint[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		return new CompressedFloat(fc, buffer.ReadUInt32(ref bitposition, bits));
	}

	public static CompressedFloat Read(this FloatCrusher fc, ulong[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bits = fc._bits[(int)bcl];
		return new CompressedFloat(fc, buffer.ReadUInt32(ref bitposition, bits));
	}

	public static float ReadAndDecompress(this FloatCrusher fc, byte[] buffer, ref int bitposition)
	{
		uint c = buffer.ReadUInt32(ref bitposition, fc._bits[0]);
		return fc.Decompress(c);
	}

	public static float ReadAndDecompress(this FloatCrusher fc, uint[] buffer, ref int bitposition)
	{
		uint c = buffer.ReadUInt32(ref bitposition, fc._bits[0]);
		return fc.Decompress(c);
	}

	public static float ReadAndDecompress(this FloatCrusher fc, ulong[] buffer, ref int bitposition)
	{
		uint c = buffer.ReadUInt32(ref bitposition, fc._bits[0]);
		return fc.Decompress(c);
	}
}
