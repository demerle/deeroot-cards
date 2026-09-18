using System;
using System.Collections.Generic;
using UnityEngine;

namespace Photon.Compression.Internal
{
	[Serializable]
	[AttributeUsage(AttributeTargets.Field)]
	public abstract class SyncVarBaseAttribute : Attribute
	{
		public SyncAs syncAs;

		public KeyRate keyRate;

		public string applyCallback;

		public string snapshotCallback;

		public SetValueTiming setValueTiming = SetValueTiming.AfterCallback;

		public bool interpolate;

		public int bitCount = -1;

		public virtual void Initialize(Type primitiveType)
		{
			if (bitCount <= -1)
			{
				bitCount = GetDefaultBitCount(primitiveType);
			}
		}

		public virtual int GetDefaultBitCount(Type fieldType)
		{
			if (fieldType == typeof(byte) || fieldType == typeof(sbyte))
			{
				return 8;
			}
			if (fieldType == typeof(ushort) || fieldType == typeof(short) || fieldType == typeof(char))
			{
				return 16;
			}
			if (fieldType == typeof(uint) || fieldType == typeof(int) || fieldType == typeof(float))
			{
				return 32;
			}
			if (fieldType == typeof(ulong) || fieldType == typeof(long) || fieldType == typeof(double))
			{
				return 64;
			}
			if (fieldType == typeof(bool))
			{
				return 1;
			}
			if (fieldType == typeof(Vector3))
			{
				return 32;
			}
			if (fieldType == typeof(Vector2))
			{
				return 32;
			}
			return 0;
		}

		public virtual int GetMaxBits(Type fieldType)
		{
			if (fieldType == typeof(byte) || fieldType == typeof(sbyte))
			{
				return 8;
			}
			if (fieldType == typeof(ushort) || fieldType == typeof(short) || fieldType == typeof(char))
			{
				return 16;
			}
			if (fieldType == typeof(uint) || fieldType == typeof(int) || fieldType == typeof(float))
			{
				return 32;
			}
			if (fieldType == typeof(ulong) || fieldType == typeof(long) || fieldType == typeof(double))
			{
				return 64;
			}
			if (fieldType == typeof(bool))
			{
				return 1;
			}
			if (fieldType == typeof(Vector3))
			{
				return 96;
			}
			if (fieldType == typeof(Vector2))
			{
				return 64;
			}
			if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>)))
			{
				Debug.LogWarning("Can't get max bits needed for List<> types, as they are variable. " + fieldType.Name);
				return 2048;
			}
			Debug.LogWarning("Can't get bits needed for unsupported types. " + fieldType.Name);
			return 2048;
		}

		public bool IsKeyframe(int frameId)
		{
			if (keyRate == KeyRate.Every)
			{
				return true;
			}
			if (keyRate == KeyRate.Never)
			{
				return false;
			}
			if (frameId % (int)keyRate == 0)
			{
				return true;
			}
			return false;
		}

		public bool IsForced(int frameId, SerializationFlags writeFlags)
		{
			if (syncAs == SyncAs.Trigger)
			{
				return false;
			}
			if (keyRate == KeyRate.Every)
			{
				return true;
			}
			if ((writeFlags & SerializationFlags.Force) != 0)
			{
				return true;
			}
			if (keyRate == KeyRate.Never && (writeFlags & SerializationFlags.NewConnection) != 0)
			{
				return true;
			}
			if (keyRate != 0 && frameId % (int)keyRate == 0)
			{
				return true;
			}
			return false;
		}

		public bool IsForcedClass<T>(int frameId, T value, T prevValue, SerializationFlags writeFlags) where T : class
		{
			if (syncAs == SyncAs.Trigger)
			{
				Debug.LogError("Reference type " + typeof(T).Name + " cannot be set to SyncAs.Trigger. This PackAttribute setting only applies to structs.");
				return true;
			}
			if (keyRate == KeyRate.Every)
			{
				return true;
			}
			if ((writeFlags & SerializationFlags.Force) != 0)
			{
				return true;
			}
			if (keyRate == KeyRate.Never && (writeFlags & SerializationFlags.NewConnection) != 0)
			{
				return true;
			}
			if (keyRate != 0 && frameId % (int)keyRate == 0)
			{
				return true;
			}
			if (!value.Equals(prevValue))
			{
				return true;
			}
			return false;
		}

		public bool IsForced<T>(int frameId, T value, T prevValue, SerializationFlags writeFlags) where T : struct
		{
			if (syncAs == SyncAs.Trigger)
			{
				return !value.Equals(new T());
			}
			if (keyRate == KeyRate.Every)
			{
				return true;
			}
			if ((writeFlags & SerializationFlags.Force) != 0)
			{
				return true;
			}
			if (keyRate == KeyRate.Never && (writeFlags & SerializationFlags.NewConnection) != 0)
			{
				return true;
			}
			if (keyRate != 0 && frameId % (int)keyRate == 0)
			{
				return true;
			}
			if (!value.Equals(prevValue))
			{
				return true;
			}
			return false;
		}
	}
}
