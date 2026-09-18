using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using emotitron.Compression;

namespace Photon.Compression
{
	[Serializable]
	public class TransformCrusher : Crusher<TransformCrusher>, ICrusherCopy<TransformCrusher>
	{
		public const int VersionMajor = 3;

		public const int VersionMinor = 5;

		public const int VersionRevision = 3;

		public const int Build = 3503;

		public static Dictionary<int, TransformCrusher> staticTransformCrushers = new Dictionary<int, TransformCrusher>();

		[HideInInspector]
		[Obsolete("Default Transform breaks crusher sharing across multiple instances and is now deprecated.")]
		[Tooltip("This is the default assumed transform when no transform or gameobject is given to methods.")]
		public Transform defaultTransform;

		[SerializeField]
		protected ElementCrusher posCrusher;

		[SerializeField]
		protected ElementCrusher rotCrusher;

		[SerializeField]
		protected ElementCrusher sclCrusher;

		[NonSerialized]
		protected readonly int[] cached_pBits = new int[4];

		[NonSerialized]
		protected readonly int[] cached_rBits = new int[4];

		[NonSerialized]
		protected readonly int[] cached_sBits = new int[4];

		[NonSerialized]
		protected readonly int[] _cached_total = new int[4];

		public ReadOnlyCollection<int> cached_total;

		protected bool cached;

		public static ulong[] reusableArray64 = new ulong[5];

		private const string transformMissingError = "The 'defaultTransform' is null and has not be set in the inspector. For non-editor usages of TransformCrusher you need to pass the target transform to this method.";

		public ElementCrusher PosCrusher
		{
			get
			{
				return posCrusher;
			}
			set
			{
				if ((object)posCrusher != value)
				{
					if (posCrusher != null)
					{
						ElementCrusher elementCrusher = posCrusher;
						elementCrusher.OnRecalculated = (Action<ElementCrusher>)Delegate.Remove(elementCrusher.OnRecalculated, new Action<ElementCrusher>(OnCrusherChange));
					}
					posCrusher = value;
					if (posCrusher != null)
					{
						ElementCrusher elementCrusher2 = posCrusher;
						elementCrusher2.OnRecalculated = (Action<ElementCrusher>)Delegate.Combine(elementCrusher2.OnRecalculated, new Action<ElementCrusher>(OnCrusherChange));
					}
					CacheValues();
				}
			}
		}

		public ElementCrusher RotCrusher
		{
			get
			{
				return rotCrusher;
			}
			set
			{
				if ((object)rotCrusher != value)
				{
					if (rotCrusher != null)
					{
						ElementCrusher elementCrusher = rotCrusher;
						elementCrusher.OnRecalculated = (Action<ElementCrusher>)Delegate.Remove(elementCrusher.OnRecalculated, new Action<ElementCrusher>(OnCrusherChange));
					}
					rotCrusher = value;
					if (rotCrusher != null)
					{
						ElementCrusher elementCrusher2 = rotCrusher;
						elementCrusher2.OnRecalculated = (Action<ElementCrusher>)Delegate.Combine(elementCrusher2.OnRecalculated, new Action<ElementCrusher>(OnCrusherChange));
					}
					CacheValues();
				}
			}
		}

		public ElementCrusher SclCrusher
		{
			get
			{
				return sclCrusher;
			}
			set
			{
				if ((object)sclCrusher != value)
				{
					if (sclCrusher != null)
					{
						ElementCrusher elementCrusher = sclCrusher;
						elementCrusher.OnRecalculated = (Action<ElementCrusher>)Delegate.Remove(elementCrusher.OnRecalculated, new Action<ElementCrusher>(OnCrusherChange));
					}
					sclCrusher = value;
					if (sclCrusher != null)
					{
						ElementCrusher elementCrusher2 = sclCrusher;
						elementCrusher2.OnRecalculated = (Action<ElementCrusher>)Delegate.Combine(elementCrusher2.OnRecalculated, new Action<ElementCrusher>(OnCrusherChange));
					}
					CacheValues();
				}
			}
		}

		public static TransformCrusher CheckAgainstStatics(TransformCrusher tc, bool CheckElementCrusherAsWell = true)
		{
			if ((object)tc == null)
			{
				return null;
			}
			if (CheckElementCrusherAsWell)
			{
				tc.posCrusher = ElementCrusher.CheckAgainstStatics(tc.posCrusher);
				tc.rotCrusher = ElementCrusher.CheckAgainstStatics(tc.rotCrusher);
				tc.sclCrusher = ElementCrusher.CheckAgainstStatics(tc.sclCrusher);
			}
			int hashCode = tc.GetHashCode();
			if (staticTransformCrushers.ContainsKey(hashCode))
			{
				return staticTransformCrushers[hashCode];
			}
			staticTransformCrushers.Add(hashCode, tc);
			return tc;
		}

		public void OnCrusherChange(ElementCrusher crusher)
		{
			CacheValues();
		}

		public TransformCrusher()
		{
			ConstructDefault();
		}

		public TransformCrusher(bool isStatic = false)
		{
			ConstructDefault(isStatic);
		}

		protected virtual void ConstructDefault(bool isStatic = false)
		{
			if (!isStatic)
			{
				PosCrusher = new ElementCrusher(TRSType.Position, false);
				RotCrusher = new ElementCrusher(TRSType.Euler, false)
				{
					XCrusher = new FloatCrusher(BitPresets.Bits12, -90f, 90f, Axis.X, TRSType.Euler, true),
					YCrusher = new FloatCrusher(BitPresets.Bits12, -180f, 180f, Axis.Y, TRSType.Euler, true),
					ZCrusher = new FloatCrusher(BitPresets.Disabled, -180f, 180f, Axis.Z, TRSType.Euler, true)
				};
				SclCrusher = new ElementCrusher(TRSType.Scale, false)
				{
					uniformAxes = ElementCrusher.UniformAxes.XYZ,
					UCrusher = new FloatCrusher(8, 0f, 2f, Axis.Uniform, TRSType.Scale, true)
				};
			}
		}

		public override void OnBeforeSerialize()
		{
		}

		public override void OnAfterDeserialize()
		{
			CacheValues();
		}

		public virtual void CacheValues()
		{
			for (int i = 0; i < 4; i++)
			{
				cached_pBits[i] = ((!(posCrusher == null)) ? posCrusher.Cached_TotalBits[i] : 0);
				cached_rBits[i] = ((!(rotCrusher == null)) ? rotCrusher.Cached_TotalBits[i] : 0);
				cached_sBits[i] = ((!(sclCrusher == null)) ? sclCrusher.Cached_TotalBits[i] : 0);
				_cached_total[i] = cached_pBits[i] + cached_rBits[i] + cached_sBits[i];
				cached_total = Array.AsReadOnly(_cached_total);
			}
			cached = true;
		}

		public void Write(CompressedMatrix cm, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (cached_pBits[(int)bcl] > 0)
			{
				posCrusher.Write(cm.cPos, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			}
			if (cached_rBits[(int)bcl] > 0)
			{
				rotCrusher.Write(cm.cRot, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			}
			if (cached_sBits[(int)bcl] > 0)
			{
				sclCrusher.Write(cm.cScl, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			}
		}

		public void Write(CompressedMatrix cm, uint[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (cached_pBits[(int)bcl] > 0)
			{
				posCrusher.Write(cm.cPos, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			}
			if (cached_rBits[(int)bcl] > 0)
			{
				rotCrusher.Write(cm.cRot, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			}
			if (cached_sBits[(int)bcl] > 0)
			{
				sclCrusher.Write(cm.cScl, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			}
		}

		public void Write(CompressedMatrix cm, ulong[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (cached_pBits[(int)bcl] > 0)
			{
				posCrusher.Write(cm.cPos, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			}
			if (cached_rBits[(int)bcl] > 0)
			{
				rotCrusher.Write(cm.cRot, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			}
			if (cached_sBits[(int)bcl] > 0)
			{
				sclCrusher.Write(cm.cScl, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			}
		}

		public void Write(CompressedMatrix nonalloc, Transform transform, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			nonalloc.crusher = this;
			if (cached_pBits[(int)bcl] > 0)
			{
				posCrusher.Write(nonalloc.cPos, transform, buffer, ref bitposition, bcl);
			}
			if (cached_rBits[(int)bcl] > 0)
			{
				rotCrusher.Write(nonalloc.cRot, transform, buffer, ref bitposition, bcl);
			}
			if (cached_sBits[(int)bcl] > 0)
			{
				sclCrusher.Write(nonalloc.cScl, transform, buffer, ref bitposition, bcl);
			}
		}

		public void Write(Transform transform, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Write(CompressedMatrix.reusable, transform, buffer, ref bitposition, bcl);
		}

		public Matrix ReadAndDecompress(ulong[] array, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return ReadAndDecompress(array, ref bitposition, bcl);
		}

		public Matrix ReadAndDecompress(uint[] array, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return ReadAndDecompress(array, ref bitposition, bcl);
		}

		public Matrix ReadAndDecompress(byte[] array, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return ReadAndDecompress(array, ref bitposition, bcl);
		}

		public void ReadAndDecompress(Matrix nonalloc, ulong[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}

		public void ReadAndDecompress(Matrix nonalloc, uint[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}

		public void ReadAndDecompress(Matrix nonalloc, byte[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}

		public Matrix ReadAndDecompress(ulong[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			ReadAndDecompress(Matrix.reusable, array, ref bitposition, bcl);
			return Matrix.reusable;
		}

		public Matrix ReadAndDecompress(uint[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			ReadAndDecompress(Matrix.reusable, array, ref bitposition, bcl);
			return Matrix.reusable;
		}

		public Matrix ReadAndDecompress(byte[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			ReadAndDecompress(Matrix.reusable, array, ref bitposition, bcl);
			return Matrix.reusable;
		}

		public void Read(CompressedMatrix nonalloc, byte[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			nonalloc.crusher = this;
			if (cached_pBits[(int)bcl] > 0)
			{
				posCrusher.Read(nonalloc.cPos, array, ref bitposition, IncludedAxes.XYZ, bcl);
			}
			if (cached_rBits[(int)bcl] > 0)
			{
				rotCrusher.Read(nonalloc.cRot, array, ref bitposition, IncludedAxes.XYZ, bcl);
			}
			if (cached_sBits[(int)bcl] > 0)
			{
				sclCrusher.Read(nonalloc.cScl, array, ref bitposition, IncludedAxes.XYZ, bcl);
			}
		}

		public CompressedMatrix Read(ulong[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			return CompressedMatrix.reusable;
		}

		public CompressedMatrix Read(uint[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			return CompressedMatrix.reusable;
		}

		public CompressedMatrix Read(byte[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			return CompressedMatrix.reusable;
		}

		public CompressedMatrix Read(ulong[] array, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			return CompressedMatrix.reusable;
		}

		public CompressedMatrix Read(uint[] array, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			return CompressedMatrix.reusable;
		}

		public CompressedMatrix Read(byte[] array, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			return CompressedMatrix.reusable;
		}

		public void Read(CompressedMatrix nonalloc, ulong[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			nonalloc.crusher = this;
			if (cached_pBits[(int)bcl] > 0)
			{
				posCrusher.Read(nonalloc.cPos, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			}
			if (cached_rBits[(int)bcl] > 0)
			{
				rotCrusher.Read(nonalloc.cRot, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			}
			if (cached_sBits[(int)bcl] > 0)
			{
				sclCrusher.Read(nonalloc.cScl, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			}
		}

		public void Read(CompressedMatrix nonalloc, uint[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			nonalloc.crusher = this;
			if (cached_pBits[(int)bcl] > 0)
			{
				posCrusher.Read(nonalloc.cPos, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			}
			if (cached_rBits[(int)bcl] > 0)
			{
				rotCrusher.Read(nonalloc.cRot, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			}
			if (cached_sBits[(int)bcl] > 0)
			{
				sclCrusher.Read(nonalloc.cScl, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			}
		}

		public void Write(CompressedMatrix nonalloc, Transform transform, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			nonalloc.crusher = this;
			if (cached_pBits[(int)bcl] > 0)
			{
				posCrusher.Write(nonalloc.cPos, transform, ref buffer, ref bitposition, bcl);
			}
			if (cached_rBits[(int)bcl] > 0)
			{
				rotCrusher.Write(nonalloc.cRot, transform, ref buffer, ref bitposition, bcl);
			}
			if (cached_sBits[(int)bcl] > 0)
			{
				sclCrusher.Write(nonalloc.cScl, transform, ref buffer, ref bitposition, bcl);
			}
		}

		public void Write(Transform transform, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (cached_pBits[(int)bcl] > 0)
			{
				posCrusher.Write(transform, ref buffer, ref bitposition, bcl);
			}
			if (cached_rBits[(int)bcl] > 0)
			{
				rotCrusher.Write(transform, ref buffer, ref bitposition, bcl);
			}
			if (cached_sBits[(int)bcl] > 0)
			{
				sclCrusher.Write(transform, ref buffer, ref bitposition, bcl);
			}
		}

		public void Write(CompressedMatrix cm, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (cached_pBits[(int)bcl] > 0)
			{
				posCrusher.Write(cm.cPos, ref buffer, ref bitposition, bcl);
			}
			if (cached_rBits[(int)bcl] > 0)
			{
				rotCrusher.Write(cm.cRot, ref buffer, ref bitposition, bcl);
			}
			if (cached_sBits[(int)bcl] > 0)
			{
				sclCrusher.Write(cm.cScl, ref buffer, ref bitposition, bcl);
			}
		}

		public void Write(CompressedMatrix nonalloc, Transform transform, ref ulong bitstream, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			Compress(nonalloc, transform);
			if (cached_pBits[(int)bcl] > 0)
			{
				posCrusher.Write(nonalloc.cPos, ref bitstream, bcl);
			}
			if (cached_rBits[(int)bcl] > 0)
			{
				rotCrusher.Write(nonalloc.cRot, ref bitstream, bcl);
			}
			if (cached_sBits[(int)bcl] > 0)
			{
				sclCrusher.Write(nonalloc.cScl, ref bitstream, bcl);
			}
		}

		public void ReadAndDecompress(Matrix nonalloc, ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			ReadAndDecompress(nonalloc, buffer, ref bitposition, bcl);
		}

		[Obsolete("Use the nonalloc overload instead and supply a target Matrix. Matrix is now a class rather than a struct")]
		public Matrix ReadAndDecompress(ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return ReadAndDecompress(buffer, ref bitposition, bcl);
		}

		public void ReadAndDecompress(Matrix nonalloc, ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}

		[Obsolete("Use the nonalloc overload instead and supply a target Matrix. Matrix is now a class rather than a struct")]
		public Matrix ReadAndDecompress(ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			return Decompress(CompressedMatrix.reusable);
		}

		public void ReadAndApply(Transform target, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Apply(target, CompressedMatrix.reusable);
		}

		public void Read(CompressedMatrix nonalloc, ulong frag0, ulong frag1 = 0uL, ulong frag2 = 0uL, ulong frag3 = 0uL, ulong frag4 = 0uL, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			int bitposition = 0;
			reusableArray64.Write(frag0, ref bitposition, 64);
			reusableArray64.Write(frag1, ref bitposition, 64);
			reusableArray64.Write(frag2, ref bitposition, 64);
			reusableArray64.Write(frag3, ref bitposition, 64);
			reusableArray64.Write(frag4, ref bitposition, 64);
			bitposition = 0;
			Read(nonalloc, reusableArray64, ref bitposition, bcl);
		}

		public void ReadAndDecompress(Matrix nonalloc, ulong frag0, ulong frag1 = 0uL, ulong frag2 = 0uL, ulong frag3 = 0uL, ulong frag4 = 0uL, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, frag0, frag1, frag2, frag3, frag4, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}

		public CompressedMatrix Read(ulong frag0, ulong frag1 = 0uL, ulong frag2 = 0uL, ulong frag3 = 0uL, uint frag4 = 0u, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, frag0, frag1, frag2, frag3, frag4, bcl);
			return CompressedMatrix.reusable;
		}

		public void Read(CompressedMatrix nonalloc, ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(nonalloc, buffer, ref bitposition, bcl);
		}

		public void Read(CompressedMatrix nonalloc, ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
			{
				CacheValues();
			}
			nonalloc.crusher = this;
			if (cached_pBits[(int)bcl] > 0)
			{
				posCrusher.Read(nonalloc.cPos, buffer, ref bitposition, bcl);
			}
			if (cached_rBits[(int)bcl] > 0)
			{
				rotCrusher.Read(nonalloc.cRot, buffer, ref bitposition, bcl);
			}
			if (cached_sBits[(int)bcl] > 0)
			{
				sclCrusher.Read(nonalloc.cScl, buffer, ref bitposition, bcl);
			}
		}

		public CompressedMatrix Read(ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			return CompressedMatrix.reusable;
		}

		[Obsolete("Supply the transform to compress. Default Transform has be deprecated.")]
		public void Compress(CompressedMatrix nonalloc)
		{
			Debug.Assert(defaultTransform, "The 'defaultTransform' is null and has not be set in the inspector. For non-editor usages of TransformCrusher you need to pass the target transform to this method.");
			Compress(nonalloc, defaultTransform);
		}

		[Obsolete("Supply the transform to compress. Default Transform has be deprecated.")]
		public CompressedMatrix Compress()
		{
			Debug.Assert(defaultTransform, "The 'defaultTransform' is null and has not be set in the inspector. For non-editor usages of TransformCrusher you need to pass the target transform to this method.");
			return Compress(defaultTransform);
		}

		public void Compress(CompressedMatrix nonalloc, Matrix matrix)
		{
			if (!cached)
			{
				CacheValues();
			}
			nonalloc.crusher = this;
			if (cached_pBits[0] > 0)
			{
				posCrusher.Compress(nonalloc.cPos, matrix.position);
			}
			else
			{
				nonalloc.cPos.Clear();
			}
			if (cached_rBits[0] > 0)
			{
				rotCrusher.Compress(nonalloc.cRot, matrix.rotation);
			}
			else
			{
				nonalloc.cRot.Clear();
			}
			if (cached_sBits[0] > 0)
			{
				sclCrusher.Compress(nonalloc.cScl, matrix.scale);
			}
			else
			{
				nonalloc.cScl.Clear();
			}
		}

		public void Compress(CompressedMatrix nonalloc, Transform transform)
		{
			if (!cached)
			{
				CacheValues();
			}
			nonalloc.crusher = this;
			if (cached_pBits[0] > 0)
			{
				posCrusher.Compress(nonalloc.cPos, transform);
			}
			else
			{
				nonalloc.cPos.Clear();
			}
			if (cached_rBits[0] > 0)
			{
				rotCrusher.Compress(nonalloc.cRot, transform);
			}
			else
			{
				nonalloc.cRot.Clear();
			}
			if (cached_sBits[0] > 0)
			{
				sclCrusher.Compress(nonalloc.cScl, transform);
			}
			else
			{
				nonalloc.cScl.Clear();
			}
		}

		public CompressedMatrix Compress(Transform transform)
		{
			Compress(CompressedMatrix.reusable, transform);
			return CompressedMatrix.reusable;
		}

		public CompressedMatrix Compress(Rigidbody rb)
		{
			Compress(CompressedMatrix.reusable, rb);
			return CompressedMatrix.reusable;
		}

		public void Compress(CompressedMatrix nonalloc, Rigidbody rb)
		{
			if (!cached)
			{
				CacheValues();
			}
			nonalloc.crusher = this;
			posCrusher.Compress(nonalloc.cPos, rb);
			rotCrusher.Compress(nonalloc.cRot, rb);
			sclCrusher.Compress(nonalloc.cScl, rb);
		}

		public CompressedMatrix Compress(Rigidbody2D rb2d)
		{
			Compress(CompressedMatrix.reusable, rb2d);
			return CompressedMatrix.reusable;
		}

		public void Compress(CompressedMatrix nonalloc, Rigidbody2D rb2d)
		{
			if (!cached)
			{
				CacheValues();
			}
			nonalloc.crusher = this;
			posCrusher.Compress(nonalloc.cPos, rb2d.transform);
			rotCrusher.Compress(nonalloc.cRot, rb2d.transform);
			sclCrusher.Compress(nonalloc.cScl, rb2d.transform);
		}

		public void CompressAndWrite(Matrix matrix, byte[] buffer, ref int bitposition)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (cached_pBits[0] > 0)
			{
				posCrusher.CompressAndWrite(matrix.position, buffer, ref bitposition);
			}
			if (cached_rBits[0] > 0)
			{
				rotCrusher.CompressAndWrite(matrix.rotation, buffer, ref bitposition);
			}
			if (cached_sBits[0] > 0)
			{
				sclCrusher.CompressAndWrite(matrix.scale, buffer, ref bitposition);
			}
		}

		public void CompressAndWrite(Transform transform, byte[] buffer, ref int bitposition)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (cached_pBits[0] > 0)
			{
				posCrusher.CompressAndWrite(transform, buffer, ref bitposition);
			}
			if (cached_rBits[0] > 0)
			{
				rotCrusher.CompressAndWrite(transform, buffer, ref bitposition);
			}
			if (cached_sBits[0] > 0)
			{
				sclCrusher.CompressAndWrite(transform, buffer, ref bitposition);
			}
		}

		public void CompressAndWrite(Rigidbody rb, byte[] buffer, ref int bitposition)
		{
			if (!cached)
			{
				CacheValues();
			}
			if (cached_pBits[0] > 0)
			{
				Vector3 v = ((posCrusher.local && (bool)rb.transform.parent) ? rb.transform.InverseTransformPoint(rb.position) : rb.position);
				posCrusher.CompressAndWrite(v, buffer, ref bitposition);
			}
			if (cached_rBits[0] > 0)
			{
				if (rotCrusher.TRSType == TRSType.Quaternion)
				{
					rotCrusher.CompressAndWrite(rb.rotation, buffer, ref bitposition);
				}
				else
				{
					rotCrusher.CompressAndWrite(rb.rotation.eulerAngles, buffer, ref bitposition);
				}
			}
			if (cached_sBits[0] > 0)
			{
				sclCrusher.CompressAndWrite(rb.transform, buffer, ref bitposition);
			}
		}

		public void Decompress(Matrix nonalloc, ulong[] buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}

		public void Decompress(Matrix nonalloc, uint[] buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}

		public void Decompress(Matrix nonalloc, ulong compressed, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, compressed, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}

		public Matrix Decompress(ulong[] buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Decompress(Matrix.reusable, CompressedMatrix.reusable);
			return Matrix.reusable;
		}

		public Matrix Decompress(uint[] buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Decompress(Matrix.reusable, CompressedMatrix.reusable);
			return Matrix.reusable;
		}

		public Matrix Decompress(byte[] buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Decompress(Matrix.reusable, CompressedMatrix.reusable);
			return Matrix.reusable;
		}

		public Matrix Decompress(ulong compressed, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, compressed, bcl);
			Decompress(Matrix.reusable, CompressedMatrix.reusable);
			return Matrix.reusable;
		}

		public void Decompress(Matrix nonalloc, CompressedMatrix compMatrix)
		{
			if (!cached)
			{
				CacheValues();
			}
			nonalloc.Set(this, (cached_pBits[0] > 0) ? ((Vector3)posCrusher.Decompress(compMatrix.cPos)) : default(Vector3), (cached_rBits[0] > 0) ? rotCrusher.Decompress(compMatrix.cRot) : ((rotCrusher.TRSType == TRSType.Quaternion) ? new Element(new Quaternion(0f, 0f, 0f, 1f)) : new Element(new Vector3(0f, 0f, 0f))), (cached_sBits[0] > 0) ? ((Vector3)sclCrusher.Decompress(compMatrix.cScl)) : default(Vector3));
		}

		[Obsolete("Use the nonalloc overload instead and supply a target Matrix. Matrix is now a class rather than a struct")]
		public Matrix Decompress(CompressedMatrix compMatrix)
		{
			if (!cached)
			{
				CacheValues();
			}
			return new Matrix(this, (cached_pBits[0] > 0) ? ((Vector3)posCrusher.Decompress(compMatrix.cPos)) : default(Vector3), (cached_rBits[0] > 0) ? rotCrusher.Decompress(compMatrix.cRot) : ((rotCrusher.TRSType == TRSType.Quaternion) ? new Element(new Quaternion(0f, 0f, 0f, 1f)) : new Element(new Vector3(0f, 0f, 0f))), (cached_sBits[0] > 0) ? ((Vector3)sclCrusher.Decompress(compMatrix.cScl)) : default(Vector3));
		}

		public void Set(Rigidbody rb, CompressedMatrix cmatrix)
		{
			if (cached_pBits[0] > 0)
			{
				posCrusher.Set(rb, cmatrix.cPos);
			}
			if (cached_rBits[0] > 0)
			{
				rotCrusher.Set(rb, cmatrix.cRot);
			}
			if (cached_sBits[0] > 0)
			{
				sclCrusher.Apply(rb.transform, cmatrix.cScl);
			}
		}

		public void Set(Rigidbody rb, Matrix matrix)
		{
			if (cached_pBits[0] > 0)
			{
				posCrusher.Set(rb, matrix.position);
			}
			if (cached_rBits[0] > 0)
			{
				rotCrusher.Set(rb, matrix.rotation);
			}
			if (cached_sBits[0] > 0)
			{
				sclCrusher.Apply(rb.transform, matrix.scale);
			}
		}

		public void Set(Rigidbody rb, ulong frag0, ulong frag1 = 0uL, ulong frag2 = 0uL, ulong frag3 = 0uL, ulong frag4 = 0uL)
		{
			Read(CompressedMatrix.reusable, frag0, frag1, frag2, frag3, frag4);
			Set(rb, CompressedMatrix.reusable);
		}

		public void Set(Rigidbody rb, ulong[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Set(rb, CompressedMatrix.reusable);
		}

		public void Set(Rigidbody rb, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Set(rb, CompressedMatrix.reusable);
		}

		public void Set(Rigidbody2D rb2d, Matrix matrix)
		{
			if (cached_pBits[0] > 0)
			{
				posCrusher.Set(rb2d, matrix.position);
			}
			if (cached_rBits[0] > 0)
			{
				rotCrusher.Set(rb2d, matrix.rotation);
			}
			if (cached_sBits[0] > 0)
			{
				sclCrusher.Apply(rb2d.transform, matrix.scale);
			}
		}

		public void Set(Rigidbody2D rb2d, CompressedMatrix cmatrix)
		{
			if (cached_pBits[0] > 0)
			{
				posCrusher.Set(rb2d, cmatrix.cPos);
			}
			if (cached_rBits[0] > 0)
			{
				rotCrusher.Set(rb2d, cmatrix.cRot);
			}
			if (cached_sBits[0] > 0)
			{
				sclCrusher.Apply(rb2d.transform, cmatrix.cScl);
			}
		}

		public void Move(Rigidbody rb, CompressedMatrix cmatrix)
		{
			Move(rb, cmatrix.Decompress());
			if (cached_pBits[0] > 0)
			{
				posCrusher.Move(rb, cmatrix.cPos);
			}
			if (cached_rBits[0] > 0)
			{
				rotCrusher.Move(rb, cmatrix.cRot);
			}
			if (cached_sBits[0] > 0)
			{
				sclCrusher.Apply(rb.transform, cmatrix.cScl);
			}
		}

		public void Move(Rigidbody rb, Matrix matrix)
		{
			if (cached_pBits[0] > 0)
			{
				posCrusher.Move(rb, matrix.position);
			}
			if (cached_rBits[0] > 0)
			{
				rotCrusher.Move(rb, matrix.rotation);
			}
			if (cached_sBits[0] > 0)
			{
				sclCrusher.Apply(rb.transform, matrix.scale);
			}
		}

		public void Move(Rigidbody rb, ulong frag0, ulong frag1 = 0uL, ulong frag2 = 0uL, ulong frag3 = 0uL, ulong frag4 = 0uL)
		{
			Read(CompressedMatrix.reusable, frag0, frag1, frag2, frag3, frag4);
			Move(rb, CompressedMatrix.reusable);
		}

		public void Move(Rigidbody rb, ulong[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Move(rb, CompressedMatrix.reusable);
		}

		public void Move(Rigidbody rb, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Move(rb, CompressedMatrix.reusable);
		}

		[Obsolete("Apply for Rigidbody has been replaced with Move and Set, to indicate usage of MovePosition/Rotation vs rb.position/rotation.")]
		public void Apply(Rigidbody rb, CompressedMatrix cmatrix)
		{
			if (cached_pBits[0] > 0)
			{
				posCrusher.Apply(rb, cmatrix.cPos);
			}
			if (cached_rBits[0] > 0)
			{
				rotCrusher.Apply(rb, cmatrix.cRot);
			}
			if (cached_sBits[0] > 0)
			{
				sclCrusher.Apply(rb.transform, cmatrix.cScl);
			}
		}

		[Obsolete("Apply for Rigidbody has been replaced with Move and Set, to indicate usage of MovePosition/Rotation vs rb.position/rotation.")]
		public void Apply(Rigidbody rb, Matrix matrix)
		{
			if (cached_pBits[0] > 0)
			{
				posCrusher.Apply(rb, matrix.position);
			}
			if (cached_rBits[0] > 0)
			{
				rotCrusher.Apply(rb, matrix.rotation);
			}
			if (cached_sBits[0] > 0)
			{
				sclCrusher.Apply(rb.transform, matrix.scale);
			}
		}

		[Obsolete("Supply the transform to compress. Default Transform has be deprecated.")]
		public void Apply(ulong cvalue)
		{
			Debug.Assert(defaultTransform, "The 'defaultTransform' is null and has not be set in the inspector. For non-editor usages of TransformCrusher you need to pass the target transform to this method.");
			Apply(defaultTransform, cvalue);
		}

		public void Apply(Transform t, ulong cvalue)
		{
			Decompress(Matrix.reusable, cvalue);
			Apply(t, Matrix.reusable);
		}

		[Obsolete("Supply the transform to compress. Default Transform has be deprecated.")]
		public void Apply(ulong u0, ulong u1, ulong u2, ulong u3, uint u4)
		{
			Debug.Assert(defaultTransform, "The 'defaultTransform' is null and has not be set in the inspector. For non-editor usages of TransformCrusher you need to pass the target transform to this method.");
			Apply(defaultTransform, u0, u1, u2, u3, u4);
		}

		public void Apply(Transform t, ulong frag0, ulong frag1 = 0uL, ulong frag2 = 0uL, ulong frag3 = 0uL, ulong frag4 = 0uL)
		{
			Read(CompressedMatrix.reusable, frag0, frag1, frag2, frag3, frag4);
			Apply(t, CompressedMatrix.reusable);
		}

		[Obsolete("Supply the transform to Apply to. Default Transform has be deprecated.")]
		public void Apply(CompressedMatrix cmatrix)
		{
			Debug.Assert(defaultTransform, "The 'defaultTransform' is null and has not be set in the inspector. For non-editor usages of TransformCrusher you need to pass the target transform to this method.");
			Apply(defaultTransform, cmatrix);
		}

		public void Apply(Transform t, CompressedMatrix cmatrix)
		{
			if (cached_pBits[0] > 0)
			{
				posCrusher.Apply(t, cmatrix.cPos);
			}
			if (cached_rBits[0] > 0)
			{
				rotCrusher.Apply(t, cmatrix.cRot);
			}
			if (cached_sBits[0] > 0)
			{
				sclCrusher.Apply(t, cmatrix.cScl);
			}
		}

		[Obsolete("Supply the transform to Apply to. Default Transform has be deprecated.")]
		public void Apply(Matrix matrix)
		{
			Debug.Assert(defaultTransform, "The 'defaultTransform' is null and has not be set in the inspector. For non-editor usages of TransformCrusher you need to pass the target transform to this method.");
			Apply(defaultTransform, matrix);
		}

		public void Apply(Transform transform, Matrix matrix)
		{
			if (cached_pBits[0] > 0)
			{
				posCrusher.Apply(transform, matrix.position);
			}
			if (cached_rBits[0] > 0)
			{
				rotCrusher.Apply(transform, matrix.rotation);
			}
			if (cached_sBits[0] > 0)
			{
				sclCrusher.Apply(transform, matrix.scale);
			}
		}

		public void Capture(Rigidbody rb, CompressedMatrix cm, Matrix m)
		{
			Compress(cm, rb);
			Decompress(m, cm);
		}

		public void Capture(Rigidbody2D rb2d, CompressedMatrix cm, Matrix m)
		{
			Compress(cm, rb2d);
			Decompress(m, cm);
		}

		public void Capture(Transform tr, CompressedMatrix cm, Matrix m)
		{
			posCrusher.Compress(cm.cPos, tr);
			m.position = (Vector3)posCrusher.Decompress(cm.cPos);
			rotCrusher.Compress(cm.cRot, tr);
			m.rotation = rotCrusher.Decompress(cm.cRot);
			sclCrusher.Compress(cm.cScl, tr);
			m.scale = (Vector3)sclCrusher.Decompress(cm.cScl);
		}

		public int TallyBits(BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int num = ((posCrusher != null) ? posCrusher.TallyBits(bcl) : 0);
			int num2 = ((posCrusher != null) ? rotCrusher.TallyBits(bcl) : 0);
			int num3 = ((posCrusher != null) ? sclCrusher.TallyBits(bcl) : 0);
			return num + num2 + num3;
		}

		public void CopyFrom(TransformCrusher source)
		{
			posCrusher.CopyFrom(source.posCrusher);
			rotCrusher.CopyFrom(source.rotCrusher);
			sclCrusher.CopyFrom(source.sclCrusher);
			CacheValues();
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as TransformCrusher);
		}

		public bool Equals(TransformCrusher other)
		{
			if (other != null && ((posCrusher == null) ? (other.posCrusher == null) : posCrusher.Equals(other.posCrusher)) && ((rotCrusher == null) ? (other.rotCrusher == null) : rotCrusher.Equals(other.rotCrusher)))
			{
				if (!(sclCrusher == null))
				{
					return sclCrusher.Equals(other.sclCrusher);
				}
				return other.sclCrusher == null;
			}
			return false;
		}

		// public override int GetHashCode()
		// {
		// 	return ((-453726296 * -1521134295 + ((!(posCrusher == null)) ? posCrusher.GetHashCode() : 0)) * -1521134295 + ((!(rotCrusher == null)) ? rotCrusher.GetHashCode() : 0)) * -1521134295 + ((!(sclCrusher == null)) ? sclCrusher.GetHashCode() : 0);
		// }

		public static bool operator ==(TransformCrusher crusher1, TransformCrusher crusher2)
		{
			return EqualityComparer<TransformCrusher>.Default.Equals(crusher1, crusher2);
		}

		public static bool operator !=(TransformCrusher crusher1, TransformCrusher crusher2)
		{
			return !(crusher1 == crusher2);
		}
	}
}
