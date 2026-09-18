using System;
using System.Collections.Generic;
using System.Text;
using Photon.Compression.Internal;
using UnityEngine;

namespace Photon.Compression
{
	public class SyncVarAttribute : SyncVarBaseAttribute, IPackByte, IPackSByte, IPackUInt16, IPackInt16, IPackUInt32, IPackInt32, IPackUInt64, IPackInt64, IPackSingle, IPackDouble, IPackString, IPackStringBuilder, IPackVector2, IPackVector3, IPackVector2Int, IPackVector3Int, IPackBoolean, IPackChar
	{
		public const int MAX_STR_LEN = 63;

		public const int STR_LEN_BITS = 6;

		public bool WholeNumbers;

		private static StringBuilder sb = new StringBuilder(0);

		public SyncVarAttribute(KeyRate keyRate = KeyRate.UseDefault)
		{
			base.keyRate = keyRate;
		}

		public override int GetMaxBits(Type fieldType)
		{
			if (fieldType == typeof(string) || fieldType == typeof(StringBuilder))
			{
				return 1030;
			}
			if (fieldType == typeof(Vector2))
			{
				return 64;
			}
			if (fieldType == typeof(Vector2))
			{
				return 96;
			}
			return base.GetMaxBits(fieldType);
		}

		public bool Compare<T>(List<T> a, List<T> b) where T : struct
		{
			int count = a.Count;
			if (count != b.Count)
			{
				return false;
			}
			for (int i = 0; i < count; i++)
			{
				if (!a[i].Equals(b[i]))
				{
					return false;
				}
			}
			return false;
		}

		public SerializationFlags Pack(ref bool value, bool prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			buffer.Write((ulong)(value ? 1 : 0), ref bitposition, 1);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref bool value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = buffer.Read(ref bitposition, 1) != 0L;
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref byte value, byte prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			buffer.Write(value, ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref byte value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = (byte)buffer.Read(ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref sbyte value, sbyte prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			buffer.WriteSigned(value, ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref sbyte value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = (sbyte)buffer.ReadSigned(ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref ushort value, ushort prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			buffer.Write(value, ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref ushort value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = (ushort)buffer.Read(ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref short value, short prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			Debug.Log("Pack " + bitCount);
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			buffer.WriteSigned(value, ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref short value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = (short)buffer.ReadSigned(ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref char value, char prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			buffer.Write(value, ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref char value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = (char)buffer.Read(ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref uint value, uint prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			buffer.WritePackedBytes(value, ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref uint value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = (uint)buffer.ReadPackedBytes(ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref int value, int prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			buffer.WriteSignedPackedBytes(value, ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref int value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = buffer.ReadSignedPackedBytes(ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref ulong value, ulong prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			buffer.WritePackedBytes(value, ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref ulong value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = buffer.ReadPackedBytes(ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref long value, long prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			buffer.WriteSignedPackedBytes64(value, ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref long value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = buffer.ReadSignedPackedBytes64(ref bitposition, bitCount);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref float value, float prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (WholeNumbers)
			{
				int value2 = (int)Math.Round(value);
				int prevValue2 = (int)Math.Round(prevValue);
				if (!IsForced(frameId, value2, prevValue2, writeFlags))
				{
					return SerializationFlags.None;
				}
				buffer.WriteSignedPackedBytes(value2, ref bitposition, 32);
				return SerializationFlags.IsComplete;
			}
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			buffer.Write((uint)(ByteConverter)value, ref bitposition, 32);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref float value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (WholeNumbers)
			{
				value = buffer.ReadSignedPackedBytes(ref bitposition, 32);
				return SerializationFlags.IsComplete;
			}
			value = (ByteConverter)buffer.Read(ref bitposition, 32);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref double value, double prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (WholeNumbers)
			{
				long value2 = (int)Math.Round(value);
				long prevValue2 = (int)Math.Round(prevValue);
				if (!IsForced(frameId, value2, prevValue2, writeFlags))
				{
					return SerializationFlags.None;
				}
				buffer.WriteSignedPackedBytes64(value2, ref bitposition, 64);
				return SerializationFlags.IsComplete;
			}
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			buffer.Write((ByteConverter)value, ref bitposition, 64);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref double value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (WholeNumbers)
			{
				value = buffer.ReadSignedPackedBytes64(ref bitposition, 64);
				return SerializationFlags.IsComplete;
			}
			value = (ByteConverter)buffer.Read(ref bitposition, 64);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref string value, string prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForcedClass(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			if (value == null)
			{
				buffer.Write(0uL, ref bitposition, 6);
				return SerializationFlags.IsComplete;
			}
			int num = value.Length;
			if (num > 63)
			{
				num = 63;
			}
			buffer.Write((uint)num, ref bitposition, 6);
			for (int i = 0; i < num; i++)
			{
				buffer.Write(value[i], ref bitposition, bitCount);
			}
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref string value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			sb.Length = 0;
			int num = (int)buffer.Read(ref bitposition, 6);
			for (int i = 0; i < num; i++)
			{
				sb.Append((char)buffer.Read(ref bitposition, bitCount));
			}
			value = sb.ToString();
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref StringBuilder value, StringBuilder prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForcedClass(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			if (value == null)
			{
				buffer.Write(0uL, ref bitposition, 6);
				return SerializationFlags.IsComplete;
			}
			int num = value.Length;
			if (num > 63)
			{
				num = 63;
			}
			buffer.Write((uint)num, ref bitposition, 6);
			for (int i = 0; i < num; i++)
			{
				buffer.Write(value[i], ref bitposition, bitCount);
			}
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref StringBuilder value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (value == null)
			{
				value = new StringBuilder(64);
			}
			else
			{
				value.Length = 0;
			}
			int num = (int)buffer.Read(ref bitposition, 6);
			for (int i = 0; i < num; i++)
			{
				value.Append((char)buffer.Read(ref bitposition, bitCount));
			}
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref Vector2 value, Vector2 prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (WholeNumbers)
			{
				Vector2Int value2 = new Vector2Int((int)value.x, (int)value.y);
				if (!IsForced<Vector2Int>(prevValue: new Vector2Int((int)prevValue.x, (int)prevValue.y), frameId: frameId, value: value2, writeFlags: writeFlags))
				{
					return SerializationFlags.None;
				}
				buffer.WriteSignedPackedBytes(value2.x, ref bitposition, 32);
				buffer.WriteSignedPackedBytes(value2.y, ref bitposition, 32);
				return SerializationFlags.IsComplete;
			}
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			buffer.Write((uint)(ByteConverter)value.x, ref bitposition, 32);
			buffer.Write((uint)(ByteConverter)value.y, ref bitposition, 32);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref Vector2 value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (WholeNumbers)
			{
				value = new Vector2(buffer.ReadSignedPackedBytes(ref bitposition, 32), buffer.ReadSignedPackedBytes(ref bitposition, 32));
				return SerializationFlags.IsComplete;
			}
			value = new Vector2((ByteConverter)buffer.Read(ref bitposition, 32), (ByteConverter)buffer.Read(ref bitposition, 32));
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref Vector3 value, Vector3 prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (WholeNumbers)
			{
				Vector3Int value2 = new Vector3Int((int)value.x, (int)value.y, (int)value.z);
				if (!IsForced<Vector3Int>(prevValue: new Vector3Int((int)prevValue.x, (int)prevValue.y, (int)prevValue.z), frameId: frameId, value: value2, writeFlags: writeFlags))
				{
					return SerializationFlags.None;
				}
				buffer.WriteSignedPackedBytes(value2.x, ref bitposition, 32);
				buffer.WriteSignedPackedBytes(value2.y, ref bitposition, 32);
				buffer.WriteSignedPackedBytes(value2.z, ref bitposition, 32);
				return SerializationFlags.IsComplete;
			}
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			buffer.Write((uint)(ByteConverter)value.x, ref bitposition, 32);
			buffer.Write((uint)(ByteConverter)value.y, ref bitposition, 32);
			buffer.Write((uint)(ByteConverter)value.z, ref bitposition, 32);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref Vector3 value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (WholeNumbers)
			{
				value = new Vector3(buffer.ReadSignedPackedBytes(ref bitposition, 32), buffer.ReadSignedPackedBytes(ref bitposition, 32), buffer.ReadSignedPackedBytes(ref bitposition, 32));
				return SerializationFlags.IsComplete;
			}
			value = new Vector3((ByteConverter)buffer.Read(ref bitposition, 32), (ByteConverter)buffer.Read(ref bitposition, 32), (ByteConverter)buffer.Read(ref bitposition, 32));
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref Vector2Int value, Vector2Int prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			buffer.WriteSignedPackedBytes(value.x, ref bitposition, 32);
			buffer.WriteSignedPackedBytes(value.y, ref bitposition, 32);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref Vector2Int value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = new Vector2Int(buffer.ReadSignedPackedBytes(ref bitposition, 32), buffer.ReadSignedPackedBytes(ref bitposition, 32));
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Pack(ref Vector3Int value, Vector3Int prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			if (!IsForced(frameId, value, prevValue, writeFlags))
			{
				return SerializationFlags.None;
			}
			buffer.WriteSignedPackedBytes(value.x, ref bitposition, 32);
			buffer.WriteSignedPackedBytes(value.y, ref bitposition, 32);
			buffer.WriteSignedPackedBytes(value.z, ref bitposition, 32);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref Vector3Int value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = new Vector3Int(buffer.ReadSignedPackedBytes(ref bitposition, 32), buffer.ReadSignedPackedBytes(ref bitposition, 32), buffer.ReadSignedPackedBytes(ref bitposition, 32));
			return SerializationFlags.IsComplete;
		}
	}
}
