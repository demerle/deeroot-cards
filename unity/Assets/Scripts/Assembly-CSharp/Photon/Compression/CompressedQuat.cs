using UnityEngine;

namespace Photon.Compression
{
	public struct CompressedQuat
	{
		public readonly QuatCrusher crusher;

		public readonly ulong cvalue;

		public CompressedQuat(QuatCrusher crusher, ulong cvalue)
		{
			this = default(CompressedQuat);
			this.crusher = crusher;
			this.cvalue = cvalue;
		}

		public CompressedQuat(QuatCrusher crusher, uint cvalue)
		{
			this = default(CompressedQuat);
			this.crusher = crusher;
			this.cvalue = cvalue;
		}

		public CompressedQuat(QuatCrusher crusher, ushort cvalue)
		{
			this = default(CompressedQuat);
			this.crusher = crusher;
			this.cvalue = cvalue;
		}

		public CompressedQuat(QuatCrusher crusher, byte cvalue)
		{
			this = default(CompressedQuat);
			this.crusher = crusher;
			this.cvalue = cvalue;
		}

		public static implicit operator ulong(CompressedQuat cv)
		{
			return cv.cvalue;
		}

		public static explicit operator uint(CompressedQuat cv)
		{
			return (uint)cv.cvalue;
		}

		public static explicit operator ushort(CompressedQuat cv)
		{
			return (ushort)cv.cvalue;
		}

		public static explicit operator byte(CompressedQuat cv)
		{
			return (byte)cv.cvalue;
		}

		public Quaternion Decompress()
		{
			return crusher.Decompress(cvalue);
		}

		public override string ToString()
		{
			return string.Concat("[CompressedQuat: ", cvalue, " bits: ", crusher, "] ");
		}
	}
}
