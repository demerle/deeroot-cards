using System;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Compression;

namespace Photon.Compression
{
	[Serializable]
	public class QuatCrusher : Crusher<QuatCrusher>, IEquatable<QuatCrusher>, ICrusherCopy<QuatCrusher>
	{
		public static bool QC_ISPRO = true;

		[Range(16f, 64f)]
		[SerializeField]
		private int bits;

		[SerializeField]
		public CompressLevel _compressLevel;

		[SerializeField]
		public Transform transform;

		[SerializeField]
		public bool local;

		[HideInInspector]
		public bool isStandalone;

		[SerializeField]
		public bool showEnableToggle;

		[SerializeField]
		private bool enabled = true;

		private QuatCompress.Cache cache;

		[NonSerialized]
		private bool initialized;

		public int Bits
		{
			get
			{
				if (!enabled)
				{
					return 0;
				}
				if (!QC_ISPRO)
				{
					return RoundBitsToBestPreset(bits);
				}
				return bits;
			}
			set
			{
				if (QC_ISPRO)
				{
					bits = value;
					CompressLevel = CompressLevel.SetBits;
				}
				else
				{
					bits = RoundBitsToBestPreset(value);
					CompressLevel = (CompressLevel)bits;
				}
				if (OnRecalculated != null)
				{
					OnRecalculated(this);
				}
			}
		}

		public CompressLevel CompressLevel
		{
			get
			{
				return _compressLevel;
			}
			set
			{
				if (QC_ISPRO)
				{
					_compressLevel = value;
					bits = ((_compressLevel == CompressLevel.SetBits) ? bits : ((int)_compressLevel));
				}
				else
				{
					if (_compressLevel == CompressLevel.SetBits)
					{
						_compressLevel = (CompressLevel)bits;
					}
					_compressLevel = (CompressLevel)RoundBitsToBestPreset((int)value);
					bits = (int)_compressLevel;
				}
				if (OnRecalculated != null)
				{
					OnRecalculated(this);
				}
			}
		}

		[SerializeField]
		public bool Enabled
		{
			get
			{
				return enabled;
			}
			set
			{
				enabled = value;
				if (OnRecalculated != null)
				{
					OnRecalculated(this);
				}
			}
		}

		public QuatCrusher()
		{
			_compressLevel = CompressLevel.uint64Hi;
			showEnableToggle = false;
			isStandalone = true;
		}

		public QuatCrusher(int bits, bool showEnableToggle = false, bool isStandalone = true)
		{
			this.bits = (QC_ISPRO ? bits : RoundBitsToBestPreset(bits));
			_compressLevel = CompressLevel.SetBits;
			this.showEnableToggle = showEnableToggle;
			this.isStandalone = isStandalone;
		}

		public QuatCrusher(bool showEnableToggle = false, bool isStandalone = true)
		{
			bits = 32;
			_compressLevel = (QC_ISPRO ? CompressLevel.SetBits : CompressLevel.uint32Med);
			this.showEnableToggle = showEnableToggle;
			this.isStandalone = isStandalone;
		}

		public QuatCrusher(CompressLevel compressLevel, bool showEnableToggle = false, bool isStandalone = true)
		{
			_compressLevel = compressLevel;
			bits = (int)compressLevel;
			this.showEnableToggle = showEnableToggle;
			this.isStandalone = isStandalone;
		}

		public void Initialize()
		{
			cache = QuatCompress.caches[Bits];
			initialized = true;
		}

		public override void OnBeforeSerialize()
		{
		}

		public override void OnAfterDeserialize()
		{
			if (OnRecalculated != null)
			{
				OnRecalculated(this);
			}
		}

		public static int RoundBitsToBestPreset(int bits)
		{
			if (bits > 32)
			{
				return 64;
			}
			if (bits > 16)
			{
				return 32;
			}
			if (bits > 8)
			{
				return 16;
			}
			return 0;
		}

		public ulong Compress()
		{
			if (!initialized)
			{
				Initialize();
			}
			if (local)
			{
				return transform.localRotation.Compress(cache);
			}
			return transform.rotation.Compress(cache);
		}

		public ulong Compress(Quaternion quat)
		{
			if (!initialized)
			{
				Initialize();
			}
			return quat.Compress(cache);
		}

		public Quaternion Decompress(ulong compressed)
		{
			if (!initialized)
			{
				Initialize();
			}
			return compressed.Decompress(cache);
		}

		public ulong Write(Quaternion quat, byte[] buffer, ref int bitposition)
		{
			ulong num = Compress(quat);
			buffer.Write(num, ref bitposition, bits);
			return num;
		}

		public ulong Write(Quaternion quat, uint[] buffer, ref int bitposition)
		{
			ulong num = Compress(quat);
			buffer.Write(num, ref bitposition, bits);
			return num;
		}

		public ulong Write(Quaternion quat, ulong[] buffer, ref int bitposition)
		{
			ulong num = Compress(quat);
			buffer.Write(num, ref bitposition, bits);
			return num;
		}

		public ulong Write(ulong c, byte[] buffer, ref int bitposition)
		{
			buffer.Write(c, ref bitposition, bits);
			return c;
		}

		public ulong Write(ulong c, uint[] buffer, ref int bitposition)
		{
			buffer.Write(c, ref bitposition, bits);
			return c;
		}

		public ulong Write(ulong c, ulong[] buffer, ref int bitposition)
		{
			buffer.Write(c, ref bitposition, bits);
			return c;
		}

		public Quaternion Read(byte[] buffer, ref int bitposition)
		{
			ulong compressed = buffer.Read(ref bitposition, bits);
			return Decompress(compressed);
		}

		public Quaternion Read(uint[] buffer, ref int bitposition)
		{
			ulong compressed = buffer.Read(ref bitposition, bits);
			return Decompress(compressed);
		}

		public Quaternion Read(ulong[] buffer, ref int bitposition)
		{
			ulong compressed = buffer.Read(ref bitposition, bits);
			return Decompress(compressed);
		}

		public ulong Write(Quaternion quat, ref ulong buffer, ref int bitposition)
		{
			ulong num = Compress(quat);
			num.Inject(ref buffer, ref bitposition, bits);
			return num;
		}

		public Quaternion Read(ref ulong buffer, ref int bitposition)
		{
			ulong compressed = buffer.Read(ref bitposition, bits);
			return Decompress(compressed);
		}

		public void CopyFrom(QuatCrusher source)
		{
			bits = source.bits;
			_compressLevel = source._compressLevel;
			local = source.local;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as QuatCrusher);
		}

		public bool Equals(QuatCrusher other)
		{
			if (other != null && bits == other.bits && _compressLevel == other._compressLevel)
			{
				return local == other.local;
			}
			return false;
		}

		// public override int GetHashCode()
		// {
		// 	return ((-282774512 * -1521134295 + bits.GetHashCode()) * -1521134295 + _compressLevel.GetHashCode()) * -1521134295 + local.GetHashCode();
		// }

		public static bool operator ==(QuatCrusher crusher1, QuatCrusher crusher2)
		{
			return EqualityComparer<QuatCrusher>.Default.Equals(crusher1, crusher2);
		}

		public static bool operator !=(QuatCrusher crusher1, QuatCrusher crusher2)
		{
			return !(crusher1 == crusher2);
		}
	}
}
