using System;
using Photon.Compression.HalfFloat;
using UnityEngine;

namespace Photon.Compression;

[Serializable]
public class LiteFloatCrusher : LiteCrusher<ulong, float>
{
	public enum Normalization
	{
		None,
		Positive,
		Negative
	}

	[SerializeField]
	public Normalization normalization;

	[SerializeField]
	protected float min;

	[SerializeField]
	protected float max;

	[SerializeField]
	public LiteFloatCompressType compressType = LiteFloatCompressType.Half16;

	[SerializeField]
	public LiteOutOfBoundsHandling outOfBoundsHandling;

	[SerializeField]
	private bool accurateCenter = true;

	[SerializeField]
	private float encoder;

	[SerializeField]
	private float decoder;

	[SerializeField]
	private ulong maxCVal;

	public LiteFloatCrusher()
	{
		compressType = LiteFloatCompressType.Half16;
		normalization = Normalization.Positive;
		min = 0f;
		max = 1f;
		accurateCenter = true;
		Recalculate(compressType, min, max, accurateCenter, this);
	}

	public LiteFloatCrusher(LiteFloatCompressType compressType, float min, float max, bool accurateCenter, LiteOutOfBoundsHandling outOfBoundsHandling = LiteOutOfBoundsHandling.Clamp)
	{
		this.compressType = compressType;
		normalization = Normalization.None;
		if (min == max)
		{
			max += 1f;
			Debug.LogWarning("Float crusher is being given min and max values that are the same. This likely is not intentional. Check your range values. Value is <i>" + min + "</i>, changing the max to " + max + " to avoid division by zero errors.");
		}
		if (min < max)
		{
			this.min = min;
			this.max = max;
		}
		else
		{
			this.min = max;
			this.max = min;
		}
		this.accurateCenter = accurateCenter;
		this.outOfBoundsHandling = outOfBoundsHandling;
		Recalculate(compressType, min, max, accurateCenter, this);
	}

	public LiteFloatCrusher(LiteFloatCompressType compressType, Normalization normalization = Normalization.None, LiteOutOfBoundsHandling outOfBoundsHandling = LiteOutOfBoundsHandling.Clamp)
	{
		this.compressType = compressType;
		this.normalization = normalization;
		switch (normalization)
		{
		case Normalization.None:
			min = 0f;
			max = 1f;
			accurateCenter = false;
			break;
		case Normalization.Positive:
			min = 0f;
			max = 1f;
			accurateCenter = false;
			break;
		case Normalization.Negative:
			min = -1f;
			max = 1f;
			accurateCenter = true;
			break;
		}
		this.outOfBoundsHandling = outOfBoundsHandling;
		Recalculate(compressType, min, max, accurateCenter, this);
	}

	public static void Recalculate(LiteFloatCompressType compressType, float min, float max, bool accurateCenter, LiteFloatCrusher crusher)
	{
		int num = (crusher.bits = (int)compressType);
		float num2 = max - min;
		ulong num3 = (ulong)((num == 64) ? (-1) : ((1L << num) - 1));
		if (accurateCenter && num3 != 0L)
		{
			num3--;
		}
		crusher.encoder = ((num2 == 0f) ? 0f : ((float)num3 / num2));
		crusher.decoder = ((num3 == 0L) ? 0f : (num2 / (float)num3));
		crusher.maxCVal = num3;
	}

	public static void Recalculate(LiteFloatCompressType compressType, float min, float max, bool accurateCenter, ref int bits, ref float encoder, ref float decoder, ref ulong maxCVal)
	{
		bits = (int)compressType;
		float num = max - min;
		ulong num2 = (ulong)((bits == 64) ? (-1) : ((1L << bits) - 1));
		if (accurateCenter && num2 != 0L)
		{
			num2--;
		}
		encoder = ((num == 0f) ? 0f : ((float)num2 / num));
		decoder = ((num2 == 0L) ? 0f : (num / (float)num2));
		maxCVal = num2;
	}

	public override ulong Encode(float val)
	{
		if (compressType == LiteFloatCompressType.Half16)
		{
			return HalfUtilities.Pack(val);
		}
		if (compressType == LiteFloatCompressType.Full32)
		{
			return ((ByteConverter)val).uint32;
		}
		float num = (val - min) * encoder + 0.5f;
		if (num < 0f)
		{
			if (outOfBoundsHandling == LiteOutOfBoundsHandling.Clamp)
			{
				return 0uL;
			}
			return maxCVal + (ulong)((long)num % 10);
		}
		if (num > (float)maxCVal)
		{
			if (outOfBoundsHandling == LiteOutOfBoundsHandling.Clamp)
			{
				return maxCVal;
			}
			return (ulong)(num % (float)maxCVal);
		}
		ulong num2 = (ulong)num;
		if (num2 <= maxCVal)
		{
			return num2;
		}
		return maxCVal;
	}

	public override float Decode(uint cval)
	{
		if (compressType == LiteFloatCompressType.Half16)
		{
			return HalfUtilities.Unpack((ushort)cval);
		}
		if (compressType == LiteFloatCompressType.Full32)
		{
			return ((ByteConverter)cval).float32;
		}
		if (cval == 0)
		{
			return min;
		}
		if (cval == maxCVal)
		{
			return max;
		}
		return (float)cval * decoder + min;
	}

	public override ulong WriteValue(float val, byte[] buffer, ref int bitposition)
	{
		if (compressType == LiteFloatCompressType.Half16)
		{
			ulong num = HalfUtilities.Pack(val);
			buffer.Write(num, ref bitposition, 16);
			return num;
		}
		if (compressType == LiteFloatCompressType.Full32)
		{
			ulong num2 = ((ByteConverter)val).uint32;
			buffer.Write(num2, ref bitposition, 32);
			return num2;
		}
		ulong num3 = Encode(val);
		buffer.Write(num3, ref bitposition, (int)compressType);
		return num3;
	}

	public override void WriteCValue(uint cval, byte[] buffer, ref int bitposition)
	{
		if (compressType == LiteFloatCompressType.Half16)
		{
			buffer.Write(cval, ref bitposition, 16);
		}
		else if (compressType == LiteFloatCompressType.Full32)
		{
			buffer.Write(cval, ref bitposition, 32);
		}
		else
		{
			buffer.Write(cval, ref bitposition, (int)compressType);
		}
	}

	public override float ReadValue(byte[] buffer, ref int bitposition)
	{
		if (compressType == LiteFloatCompressType.Half16)
		{
			return HalfUtilities.Unpack((ushort)buffer.Read(ref bitposition, 16));
		}
		if (compressType == LiteFloatCompressType.Full32)
		{
			return ((ByteConverter)(uint)buffer.Read(ref bitposition, 32)).float32;
		}
		uint val = (uint)buffer.Read(ref bitposition, (int)compressType);
		return Decode(val);
	}

	public override ulong ReadCValue(byte[] buffer, ref int bitposition)
	{
		if (compressType == LiteFloatCompressType.Half16)
		{
			return (ushort)buffer.Read(ref bitposition, 16);
		}
		if (compressType == LiteFloatCompressType.Full32)
		{
			return (uint)buffer.Read(ref bitposition, 32);
		}
		return (uint)buffer.Read(ref bitposition, (int)compressType);
	}
}
