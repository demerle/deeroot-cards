using System;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Compression;

namespace Photon.Compression
{
	public class CompressedMatrix : IEquatable<CompressedMatrix>
	{
		public CompressedElement cPos = new CompressedElement();

		public CompressedElement cRot = new CompressedElement();

		public CompressedElement cScl = new CompressedElement();

		public TransformCrusher crusher;

		public static CompressedMatrix reusable = new CompressedMatrix();

		protected static readonly ulong[] reusableArray64 = new ulong[6];

		protected static readonly uint[] reusableArray32 = new uint[12];

		protected static readonly byte[] reusableArray8 = new byte[24];

		public CompressedMatrix()
		{
		}

		public CompressedMatrix(TransformCrusher crusher)
		{
			this.crusher = crusher;
		}

		public CompressedMatrix(TransformCrusher crusher, CompressedElement cPos, CompressedElement cRot, CompressedElement cScl)
		{
			this.crusher = crusher;
			this.cPos = cPos;
			this.cRot = cRot;
			this.cScl = cScl;
		}

		public CompressedMatrix(TransformCrusher crusher, ref CompressedElement cPos, ref CompressedElement cRot, ref CompressedElement cScl, int pBits, int rBits, int sBits)
		{
			this.crusher = crusher;
			this.cPos = cPos;
			this.cRot = cRot;
			this.cScl = cScl;
		}

		public void CopyTo(CompressedMatrix copyTarget)
		{
			cPos.CopyTo(copyTarget.cPos);
			cRot.CopyTo(copyTarget.cRot);
			cScl.CopyTo(copyTarget.cScl);
		}

		public void CopyFrom(CompressedMatrix copySource)
		{
			cPos.CopyFrom(copySource.cPos);
			cRot.CopyFrom(copySource.cRot);
			cScl.CopyFrom(copySource.cScl);
		}

		public void Clear()
		{
			crusher = null;
			cPos.Clear();
			cRot.Clear();
			cScl.Clear();
		}

		public ulong[] AsArray64(BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			crusher.Write(this, reusableArray64, ref bitposition, bcl);
			reusableArray64.Zero(bitposition + 63 >> 6);
			return reusableArray64;
		}

		public void AsArray64(ulong[] nonalloc, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			crusher.Write(this, nonalloc, ref bitposition, bcl);
			nonalloc.Zero(bitposition + 63 >> 6);
		}

		public uint[] AsArray32(BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			crusher.Write(this, reusableArray32, ref bitposition, bcl);
			reusableArray32.Zero(bitposition + 31 >> 5);
			return reusableArray32;
		}

		public void AsArray32(uint[] nonalloc, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			crusher.Write(this, nonalloc, ref bitposition, bcl);
			nonalloc.Zero(bitposition + 31 >> 5);
		}

		public byte[] AsArray8(BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			crusher.Write(this, reusableArray64, ref bitposition, bcl);
			reusableArray8.Zero(bitposition + 7 >> 3);
			return reusableArray8;
		}

		public void AsArray8(byte[] nonalloc, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			crusher.Write(this, nonalloc, ref bitposition, bcl);
			nonalloc.Zero(bitposition + 7 >> 3);
		}

		public static explicit operator ulong(CompressedMatrix cm)
		{
			ulong buffer = 0uL;
			int bitposition = 0;
			cm.crusher.Write(cm, ref buffer, ref bitposition);
			return buffer;
		}

		public static explicit operator uint(CompressedMatrix cm)
		{
			ulong buffer = 0uL;
			int bitposition = 0;
			cm.crusher.Write(cm, ref buffer, ref bitposition);
			return (uint)buffer;
		}

		public static explicit operator ushort(CompressedMatrix cm)
		{
			ulong buffer = 0uL;
			int bitposition = 0;
			cm.crusher.Write(cm, ref buffer, ref bitposition);
			return (ushort)buffer;
		}

		public static explicit operator byte(CompressedMatrix cm)
		{
			ulong buffer = 0uL;
			int bitposition = 0;
			cm.crusher.Write(cm, ref buffer, ref bitposition);
			return (byte)buffer;
		}

		public static explicit operator ulong[](CompressedMatrix cm)
		{
			return cm.AsArray64();
		}

		public static explicit operator uint[](CompressedMatrix cm)
		{
			return cm.AsArray32();
		}

		public static explicit operator byte[](CompressedMatrix cm)
		{
			return cm.AsArray8();
		}

		public void Decompress(Matrix nonalloc)
		{
			if (crusher != null)
			{
				crusher.Decompress(nonalloc, this);
			}
			else
			{
				nonalloc.Clear();
			}
		}

		public Matrix Decompress()
		{
			crusher.Decompress(Matrix.reusable, this);
			return Matrix.reusable;
		}

		[Obsolete("Supply the transform to Compress. Default Transform has been deprecated to allow shared TransformCrushers.")]
		public void Apply()
		{
			if (crusher != null)
			{
				crusher.Apply(this);
			}
		}

		public void Apply(Transform t)
		{
			if (crusher != null)
			{
				crusher.Apply(t, this);
			}
		}

		[Obsolete("Apply for Rigidbody has been replaced with Move and Set, to indicate usage of MovePosition/Rotation vs rb.position/rotation.")]
		public void Apply(Rigidbody rb)
		{
			if (crusher != null)
			{
				crusher.Apply(rb, this);
			}
		}

		public void Set(Rigidbody rb)
		{
			if (crusher != null)
			{
				crusher.Set(rb, this);
			}
		}

		public void Move(Rigidbody rb)
		{
			if (crusher != null)
			{
				crusher.Move(rb, this);
			}
		}

		public static bool operator ==(CompressedMatrix a, CompressedMatrix b)
		{
			if ((object)a == null)
			{
				return false;
			}
			return a.Equals(b);
		}

		public static bool operator !=(CompressedMatrix a, CompressedMatrix b)
		{
			if ((object)a == null)
			{
				return true;
			}
			return !a.Equals(b);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as CompressedMatrix);
		}

		public bool Equals(CompressedMatrix other)
		{
			if ((object)other != null && cPos.Equals(other.cPos) && cRot.Equals(other.cRot))
			{
				return cScl.Equals(other.cScl);
			}
			return false;
		}

		// public override int GetHashCode()
		// {
		// 	return (((94804922 * -1521134295 + cPos.GetHashCode()) * -1521134295 + cRot.GetHashCode()) * -1521134295 + cScl.GetHashCode()) * -1521134295 + EqualityComparer<TransformCrusher>.Default.GetHashCode(crusher);
		// }
	}
}
