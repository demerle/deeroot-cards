using System;
using System.Runtime.InteropServices;
using UnityEngine;
using emotitron.Compression;

namespace Photon.Compression;

[StructLayout(LayoutKind.Explicit)]
public class CompressedElement : IEquatable<CompressedElement>
{
	public static CompressedElement reusable = new CompressedElement();

	[FieldOffset(0)]
	public CompressedFloat cx;

	[FieldOffset(16)]
	public CompressedFloat cy;

	[FieldOffset(32)]
	public CompressedFloat cz;

	[FieldOffset(0)]
	public CompressedFloat cUniform;

	[FieldOffset(0)]
	public CompressedQuat cQuat;

	[FieldOffset(48)]
	public ElementCrusher crusher;

	private static readonly ulong[] reusableArray64 = new ulong[2];

	private static readonly uint[] reusableArray32 = new uint[4];

	private static readonly byte[] reusableArray8 = new byte[16];

	[Obsolete("Compressed Element is now a class and no longer a struct. Where this used to be used, now compressedElement.Clear() should be used instead.")]
	public static readonly CompressedElement Empty = new CompressedElement();

	private static CompressedElement uppers = new CompressedElement();

	private static CompressedElement lowers = new CompressedElement();

	public uint this[int axis] => axis switch
	{
		1 => cy, 
		0 => cx, 
		_ => cz, 
	};

	public void Clear()
	{
		crusher = null;
		cx = new CompressedFloat(null, 0);
		cy = new CompressedFloat(null, 0);
		cz = new CompressedFloat(null, 0);
	}

	public ulong[] AsArray64(BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bitposition = 0;
		reusableArray64.Append(cx.cvalue, ref bitposition, cx.crusher._bits[(int)bcl]);
		reusableArray64.Append(cy.cvalue, ref bitposition, cy.crusher._bits[(int)bcl]);
		reusableArray64.Append(cz.cvalue, ref bitposition, cz.crusher._bits[(int)bcl]);
		reusableArray64.Zero(bitposition + 63 >> 6);
		return reusableArray64;
	}

	public void AsArray64(ulong[] nonalloc, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bitposition = 0;
		nonalloc.Append(cx.cvalue, ref bitposition, cx.crusher._bits[(int)bcl]);
		nonalloc.Append(cy.cvalue, ref bitposition, cy.crusher._bits[(int)bcl]);
		nonalloc.Append(cz.cvalue, ref bitposition, cz.crusher._bits[(int)bcl]);
		nonalloc.Zero(bitposition + 63 >> 6);
	}

	public uint[] AsArray32(BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bitposition = 0;
		reusableArray64.Append(cx.cvalue, ref bitposition, cx.crusher._bits[(int)bcl]);
		reusableArray64.Append(cy.cvalue, ref bitposition, cy.crusher._bits[(int)bcl]);
		reusableArray64.Append(cz.cvalue, ref bitposition, cz.crusher._bits[(int)bcl]);
		reusableArray64.Zero(bitposition + 31 >> 5);
		return reusableArray32;
	}

	public void AsArray32(uint[] nonalloc, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bitposition = 0;
		nonalloc.Append(cx.cvalue, ref bitposition, cx.crusher._bits[(int)bcl]);
		nonalloc.Append(cy.cvalue, ref bitposition, cy.crusher._bits[(int)bcl]);
		nonalloc.Append(cz.cvalue, ref bitposition, cz.crusher._bits[(int)bcl]);
		nonalloc.Zero(bitposition + 31 >> 5);
	}

	public byte[] AsArray8(BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bitposition = 0;
		reusableArray8.Append(cx.cvalue, ref bitposition, cx.crusher._bits[(int)bcl]);
		reusableArray8.Append(cy.cvalue, ref bitposition, cy.crusher._bits[(int)bcl]);
		reusableArray8.Append(cz.cvalue, ref bitposition, cz.crusher._bits[(int)bcl]);
		reusableArray64.Zero(bitposition + 7 >> 3);
		return reusableArray8;
	}

	public void AsArray8(byte[] nonalloc, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		int bitposition = 0;
		nonalloc.Append(cx.cvalue, ref bitposition, cx.crusher._bits[(int)bcl]);
		nonalloc.Append(cy.cvalue, ref bitposition, cy.crusher._bits[(int)bcl]);
		nonalloc.Append(cz.cvalue, ref bitposition, cz.crusher._bits[(int)bcl]);
		nonalloc.Zero(bitposition + 7 >> 3);
	}

	public static explicit operator ulong(CompressedElement ce)
	{
		ulong buffer = 0uL;
		ce.crusher.Write(ce, ref buffer);
		return buffer;
	}

	public static explicit operator uint(CompressedElement ce)
	{
		ulong buffer = 0uL;
		ce.crusher.Write(ce, ref buffer);
		return (uint)buffer;
	}

	public static explicit operator ushort(CompressedElement ce)
	{
		ulong buffer = 0uL;
		ce.crusher.Write(ce, ref buffer);
		return (ushort)buffer;
	}

	public static explicit operator byte(CompressedElement ce)
	{
		ulong buffer = 0uL;
		ce.crusher.Write(ce, ref buffer);
		return (byte)buffer;
	}

	public static explicit operator ulong[](CompressedElement ce)
	{
		ce.AsArray64(reusableArray64);
		return reusableArray64;
	}

	public static explicit operator uint[](CompressedElement ce)
	{
		ce.AsArray32(reusableArray32);
		return reusableArray32;
	}

	public static explicit operator byte[](CompressedElement ce)
	{
		ce.AsArray8(reusableArray8);
		return reusableArray8;
	}

	public static explicit operator Element(CompressedElement ce)
	{
		return ce.Decompress();
	}

	public static explicit operator Vector3(CompressedElement ce)
	{
		Element element = ce.Decompress();
		if (ce.crusher.TRSType == TRSType.Quaternion)
		{
			Debug.LogWarning("Casting CompressedElement of type Quaternion to a Vector3 using quaternion.eulerAngles. Is this intentional? Cast to Quaternion and convert to eulerAnges yourself to silence this warning.");
			return element.quat.eulerAngles;
		}
		return element.v;
	}

	public static explicit operator Quaternion(CompressedElement ce)
	{
		Element element = ce.Decompress();
		TRSType tRSType = ce.crusher.TRSType;
		switch (tRSType)
		{
		case TRSType.Quaternion:
			return element.quat;
		case TRSType.Euler:
			Debug.LogWarning("Casting a CompressedElement of TRSType.Euler to a Quaternion using Quaternion.Euler(). Is this intentional? Cast to Vector3 and convert to Quaternion yourself to silence this warning.");
			return Quaternion.Euler(element.v);
		default:
			Debug.LogError(string.Concat("Trying to cast a CompresedElement of ", tRSType, " to a quaternion, even though it is not a rotation type. Are you using the correct ElementCrusher to compressed this value?"));
			return element.quat;
		}
	}

	public CompressedElement()
	{
	}

	public CompressedElement(ElementCrusher crusher, CompressedFloat cx, CompressedFloat cy, CompressedFloat cz)
	{
		this.crusher = crusher;
		this.cx = cx;
		this.cy = cy;
		this.cz = cz;
	}

	public CompressedElement(ElementCrusher crusher, uint cx, uint cy, uint cz)
	{
		UnityEngine.Debug.LogWarning("CE Construct");
		this.crusher = crusher;
		this.cx = new CompressedFloat(crusher.XCrusher, cx);
		this.cy = new CompressedFloat(crusher.YCrusher, cy);
		this.cz = new CompressedFloat(crusher.ZCrusher, cz);
	}

	public CompressedElement(ElementCrusher crusher, uint cUniform)
	{
		UnityEngine.Debug.LogWarning("CE Construct");
		this.crusher = crusher;
		this.cUniform = new CompressedFloat(crusher.UCrusher, cUniform);
	}

	public CompressedElement(ElementCrusher crusher, ulong cQuat)
	{
		UnityEngine.Debug.LogWarning("CE Construct");
		this.crusher = crusher;
		this.cQuat = new CompressedQuat(crusher.QCrusher, cQuat);
	}

	public void Set(ElementCrusher crusher, CompressedFloat cx, CompressedFloat cy, CompressedFloat cz)
	{
		this.crusher = crusher;
		this.cx = cx;
		this.cy = cy;
		this.cz = cz;
	}

	public void Set(ElementCrusher crusher, uint cx, uint cy, uint cz)
	{
		this.crusher = crusher;
		this.cx = new CompressedFloat(crusher.XCrusher, cx);
		this.cy = new CompressedFloat(crusher.YCrusher, cy);
		this.cz = new CompressedFloat(crusher.ZCrusher, cz);
	}

	public void Set(ElementCrusher crusher, uint cUniform)
	{
		this.crusher = crusher;
		this.cUniform = new CompressedFloat(crusher.UCrusher, cUniform);
	}

	public void Set(ElementCrusher crusher, ulong cQuat)
	{
		this.crusher = crusher;
		this.cQuat = new CompressedQuat(crusher.QCrusher, cQuat);
	}

	public void CopyTo(CompressedElement copyTarget)
	{
		copyTarget.crusher = crusher;
		copyTarget.cx = cx;
		copyTarget.cy = cy;
		copyTarget.cz = cz;
	}

	public void CopyFrom(CompressedElement copySource)
	{
		crusher = copySource.crusher;
		cx = copySource.cx;
		cy = copySource.cy;
		cz = copySource.cz;
	}

	public uint GetUInt(int axis)
	{
		return axis switch
		{
			1 => cy, 
			0 => cx, 
			_ => cz, 
		};
	}

	public Element Decompress()
	{
		return crusher.Decompress(this);
	}

	public void Serialize(byte[] buffer, ref int bitposition, IncludedAxes ia, BitCullingLevel bcl = BitCullingLevel.NoCulling)
	{
		crusher.Write(this, buffer, ref bitposition, ia, bcl);
	}

	[Obsolete("Use a.Compare(b) now instead.")]
	public static bool Compare(CompressedElement a, CompressedElement b)
	{
		return a.Equals(b);
	}

	public static int HighestDifferentBit(uint a, uint b)
	{
		int result = 0;
		for (int i = 0; i < 32; i++)
		{
			uint num = (uint)(1 << i);
			if ((a & num) == (b & num))
			{
				result = i;
			}
		}
		return result;
	}

	public static void Extrapolate(ElementCrusher crusher, CompressedElement target, CompressedElement curr, CompressedElement prev, int divisor = 2)
	{
		target.Set(crusher, (uint)((uint)curr.cx + ((long)(uint)curr.cx - (long)(uint)prev.cx) / divisor), (uint)((uint)curr.cy + ((long)(uint)curr.cy - (long)(uint)prev.cy) / divisor), (uint)((uint)curr.cz + ((long)(uint)curr.cz - (long)(uint)prev.cz) / divisor));
	}

	[Obsolete]
	public static CompressedElement Extrapolate(ElementCrusher crusher, CompressedElement curr, CompressedElement prev, int divisor = 2)
	{
		return new CompressedElement(crusher, (uint)((uint)curr.cx + ((long)(uint)curr.cx - (long)(uint)prev.cx) / divisor), (uint)((uint)curr.cy + ((long)(uint)curr.cy - (long)(uint)prev.cy) / divisor), (uint)((uint)curr.cz + ((long)(uint)curr.cz - (long)(uint)prev.cz) / divisor));
	}

	public static void Extrapolate(CompressedElement target, CompressedElement curr, CompressedElement prev, int divisor = 2)
	{
		target.Set(curr.crusher, (uint)((uint)curr.cx + ((long)(uint)curr.cx - (long)(uint)prev.cx) / divisor), (uint)((uint)curr.cy + ((long)(uint)curr.cy - (long)(uint)prev.cy) / divisor), (uint)((uint)curr.cz + ((long)(uint)curr.cz - (long)(uint)prev.cz) / divisor));
	}

	[Obsolete]
	public static CompressedElement Extrapolate(CompressedElement curr, CompressedElement prev, int divisor = 2)
	{
		return new CompressedElement(curr.crusher, (uint)((uint)curr.cx + ((long)(uint)curr.cx - (long)(uint)prev.cx) / divisor), (uint)((uint)curr.cy + ((long)(uint)curr.cy - (long)(uint)prev.cy) / divisor), (uint)((uint)curr.cz + ((long)(uint)curr.cz - (long)(uint)prev.cz) / divisor));
	}

	[Obsolete]
	public static void Extrapolate(ElementCrusher crusher, CompressedElement target, CompressedElement curr, CompressedElement prev, float amount = 0.5f)
	{
		target.Set(crusher, (uint)((float)(ulong)curr.cx + (float)((long)(uint)curr.cx - (long)(uint)prev.cx) * amount), (uint)((float)(ulong)curr.cy + (float)((long)(uint)curr.cy - (long)(uint)prev.cy) * amount), (uint)((float)(ulong)curr.cz + (float)((long)(uint)curr.cz - (long)(uint)prev.cz) * amount));
	}

	[Obsolete]
	public static CompressedElement Extrapolate(ElementCrusher crusher, CompressedElement curr, CompressedElement prev, float amount = 0.5f)
	{
		return new CompressedElement(crusher, (uint)((float)(ulong)curr.cx + (float)((long)(uint)curr.cx - (long)(uint)prev.cx) * amount), (uint)((float)(ulong)curr.cy + (float)((long)(uint)curr.cy - (long)(uint)prev.cy) * amount), (uint)((float)(ulong)curr.cz + (float)((long)(uint)curr.cz - (long)(uint)prev.cz) * amount));
	}

	public static void Extrapolate(CompressedElement target, CompressedElement curr, CompressedElement prev, float amount = 0.5f)
	{
		target.Set(curr.crusher, (uint)((float)(ulong)curr.cx + (float)((long)(uint)curr.cx - (long)(uint)prev.cx) * amount), (uint)((float)(ulong)curr.cy + (float)((long)(uint)curr.cy - (long)(uint)prev.cy) * amount), (uint)((float)(ulong)curr.cz + (float)((long)(uint)curr.cz - (long)(uint)prev.cz) * amount));
	}

	[Obsolete]
	public static CompressedElement Extrapolate(CompressedElement curr, CompressedElement prev, float amount = 0.5f)
	{
		return new CompressedElement(curr.crusher, (uint)((float)(ulong)curr.cx + (float)((long)(uint)curr.cx - (long)(uint)prev.cx) * amount), (uint)((float)(ulong)curr.cy + (float)((long)(uint)curr.cy - (long)(uint)prev.cy) * amount), (uint)((float)(ulong)curr.cz + (float)((long)(uint)curr.cz - (long)(uint)prev.cz) * amount));
	}

	public static BitCullingLevel GetGuessableBitCullLevel(CompressedElement a, CompressedElement b, BitCullingLevel maxCullLvl)
	{
		for (BitCullingLevel bitCullingLevel = maxCullLvl; bitCullingLevel > BitCullingLevel.NoCulling; bitCullingLevel--)
		{
			a.ZeroLowerBits(uppers, bitCullingLevel);
			b.ZeroUpperBits(lowers, bitCullingLevel);
			if (((ushort)uppers.cx | (ushort)lowers.cx) == (ushort)b.cx && ((ushort)uppers.cy | (ushort)lowers.cy) == (ushort)b.cy && ((ushort)uppers.cz | (ushort)lowers.cz) == (ushort)b.cz)
			{
				return bitCullingLevel;
			}
		}
		return BitCullingLevel.NoCulling;
	}

	[Obsolete]
	public static BitCullingLevel GetGuessableBitCullLevel(CompressedElement oldComp, CompressedElement newComp, ElementCrusher ec, BitCullingLevel maxCullLvl)
	{
		for (BitCullingLevel bitCullingLevel = maxCullLvl; bitCullingLevel > BitCullingLevel.NoCulling; bitCullingLevel--)
		{
			oldComp.ZeroLowerBits(uppers, bitCullingLevel);
			newComp.ZeroUpperBits(lowers, bitCullingLevel);
			if (((ushort)uppers.cx | (ushort)lowers.cx) == (ushort)newComp.cx && ((ushort)uppers.cy | (ushort)lowers.cy) == (ushort)newComp.cy && ((ushort)uppers.cz | (ushort)lowers.cz) == (ushort)newComp.cz)
			{
				return bitCullingLevel;
			}
		}
		return BitCullingLevel.NoCulling;
	}

	public static BitCullingLevel FindBestBitCullLevel(CompressedElement a, CompressedElement b, BitCullingLevel maxCulling)
	{
		ElementCrusher elementCrusher = a.crusher;
		if (elementCrusher == null)
		{
			UnityEngine.Debug.Log("NUL CE CRUSHER FindBestBitCullLevel");
			return BitCullingLevel.NoCulling;
		}
		if (elementCrusher.TRSType == TRSType.Quaternion)
		{
			if ((ulong)a.cQuat == (ulong)b.cQuat)
			{
				return BitCullingLevel.DropAll;
			}
			return BitCullingLevel.NoCulling;
		}
		if (maxCulling == BitCullingLevel.NoCulling || !TestMatchingUpper(a, b, BitCullingLevel.DropThird))
		{
			return BitCullingLevel.NoCulling;
		}
		if (maxCulling == BitCullingLevel.DropThird || !TestMatchingUpper(a, b, BitCullingLevel.DropHalf))
		{
			return BitCullingLevel.DropThird;
		}
		if (maxCulling == BitCullingLevel.DropHalf || !TestMatchingUpper(a, b, BitCullingLevel.DropAll))
		{
			return BitCullingLevel.DropHalf;
		}
		return BitCullingLevel.DropAll;
	}

	[Obsolete]
	public static BitCullingLevel FindBestBitCullLevel(CompressedElement a, CompressedElement b, ElementCrusher ec, BitCullingLevel maxCulling)
	{
		if (ec.TRSType == TRSType.Quaternion)
		{
			if ((ulong)a.cQuat == (ulong)b.cQuat)
			{
				return BitCullingLevel.DropAll;
			}
			return BitCullingLevel.NoCulling;
		}
		if (maxCulling == BitCullingLevel.NoCulling || !TestMatchingUpper(a, b, ec, BitCullingLevel.DropThird))
		{
			return BitCullingLevel.NoCulling;
		}
		if (maxCulling == BitCullingLevel.DropThird || !TestMatchingUpper(a, b, ec, BitCullingLevel.DropHalf))
		{
			return BitCullingLevel.DropThird;
		}
		if (maxCulling == BitCullingLevel.DropHalf || !TestMatchingUpper(a, b, ec, BitCullingLevel.DropAll))
		{
			return BitCullingLevel.DropHalf;
		}
		return BitCullingLevel.DropAll;
	}

	[Obsolete]
	public static BitCullingLevel FindBestBitCullLevel(CompressedElement a, CompressedElement b, FloatCrusher[] ec, BitCullingLevel maxCulling)
	{
		if (maxCulling == BitCullingLevel.NoCulling || !TestMatchingUpper(a, b, ec, BitCullingLevel.DropThird))
		{
			return BitCullingLevel.NoCulling;
		}
		if (maxCulling == BitCullingLevel.DropThird || !TestMatchingUpper(a, b, ec, BitCullingLevel.DropHalf))
		{
			return BitCullingLevel.DropThird;
		}
		if (maxCulling == BitCullingLevel.DropHalf || !TestMatchingUpper(a, b, ec, BitCullingLevel.DropAll))
		{
			return BitCullingLevel.DropHalf;
		}
		return BitCullingLevel.DropAll;
	}

	private static bool TestMatchingUpper(uint a, uint b, int lowerbits)
	{
		return a >> lowerbits << lowerbits == b >> lowerbits << lowerbits;
	}

	public static bool TestMatchingUpper(CompressedElement a, CompressedElement b, BitCullingLevel bcl)
	{
		ElementCrusher elementCrusher = a.crusher;
		if (TestMatchingUpper(a.cx, b.cx, elementCrusher.XCrusher.GetBits(bcl)) && TestMatchingUpper(a.cy, b.cy, elementCrusher.YCrusher.GetBits(bcl)))
		{
			return TestMatchingUpper(a.cz, b.cz, elementCrusher.ZCrusher.GetBits(bcl));
		}
		return false;
	}

	[Obsolete]
	public static bool TestMatchingUpper(CompressedElement a, CompressedElement b, ElementCrusher ec, BitCullingLevel bcl)
	{
		if (TestMatchingUpper(a.cx, b.cx, ec[0].GetBits(bcl)) && TestMatchingUpper(a.cy, b.cy, ec[1].GetBits(bcl)))
		{
			return TestMatchingUpper(a.cz, b.cz, ec[2].GetBits(bcl));
		}
		return false;
	}

	[Obsolete]
	public static bool TestMatchingUpper(CompressedElement a, CompressedElement b, FloatCrusher[] ec, BitCullingLevel bcl)
	{
		if (TestMatchingUpper(a.cx, b.cx, ec[0].GetBitsAtCullLevel(bcl)) && TestMatchingUpper(a.cy, b.cy, ec[1].GetBitsAtCullLevel(bcl)))
		{
			return TestMatchingUpper(a.cz, b.cz, ec[2].GetBitsAtCullLevel(bcl));
		}
		return false;
	}

	public override string ToString()
	{
		if (crusher == null)
		{
			return "[Empty CompElement]";
		}
		if (crusher.TRSType == TRSType.Quaternion)
		{
			return string.Concat(crusher.TRSType, " [", cQuat.cvalue, "]");
		}
		if (crusher.TRSType == TRSType.Scale && crusher.uniformAxes != ElementCrusher.UniformAxes.NonUniform)
		{
			return string.Concat(crusher.TRSType, " [", crusher.uniformAxes, " : ", cUniform.cvalue, "]");
		}
		return string.Concat(crusher.TRSType, " [x:", cx.cvalue, " y:", cy.cvalue, " z:", cz.cvalue, "]");
	}

	public static bool operator ==(CompressedElement a, CompressedElement b)
	{
		return a?.Equals(b) ?? false;
	}

	public static bool operator !=(CompressedElement a, CompressedElement b)
	{
		if ((object)a == null)
		{
			return true;
		}
		return !a.Equals(b);
	}

	public override bool Equals(object obj)
	{
		return Equals(obj as CompressedElement);
	}

	public bool Equals(CompressedElement other)
	{
		if ((object)other != null && cx.cvalue == other.cx.cvalue && cy.cvalue == other.cy.cvalue && cz.cvalue == other.cz.cvalue && cUniform.cvalue == other.cUniform.cvalue)
		{
			return cQuat.cvalue == other.cQuat.cvalue;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((((-1337834834 * -1521134295 + cx.GetHashCode()) * -1521134295 + cy.GetHashCode()) * -1521134295 + cz.GetHashCode()) * -1521134295 + cUniform.GetHashCode()) * -1521134295 + cQuat.GetHashCode();
	}
}
