using System.Collections;
using System.Collections.Generic;
using Photon.Compression.Internal;

namespace Photon.Compression;

public class SyncListAttribute : SyncVarBaseAttribute, IPackList<int>
{
	public SerializationFlags Pack(ref List<int> value, List<int> prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
	{
		bool flag = IsKeyframe(frameId);
		bool flag2 = (writeFlags & (SerializationFlags)22) != 0;
		SerializationFlags serializationFlags = SerializationFlags.None;
		int num = bitposition;
		int i = 0;
		for (int count = value.Count; i < count; i++)
		{
			int num2 = value[i];
			if (!flag)
			{
				if (!flag2 && num2 == prevValue[i])
				{
					buffer.WriteBool(b: false, ref bitposition);
					continue;
				}
				buffer.WriteBool(b: true, ref bitposition);
			}
			buffer.WriteSignedPackedBytes(num2, ref bitposition, bitCount);
			serializationFlags |= SerializationFlags.HasContent;
		}
		if (serializationFlags == SerializationFlags.None)
		{
			bitposition = num;
		}
		return serializationFlags;
	}

	public SerializationFlags Unpack(ref List<int> value, BitArray isCompleteMask, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
	{
		bool flag = IsKeyframe(frameId);
		SerializationFlags serializationFlags = SerializationFlags.None;
		SerializationFlags serializationFlags2 = SerializationFlags.IsComplete;
		int i = 0;
		for (int count = value.Count; i < count; i++)
		{
			if (!flag && !buffer.ReadBool(ref bitposition))
			{
				serializationFlags2 = SerializationFlags.None;
				isCompleteMask[i] = false;
			}
			else
			{
				isCompleteMask[i] = true;
				value[i] = buffer.ReadSignedPackedBytes(ref bitposition, bitCount);
				serializationFlags |= SerializationFlags.HasContent;
			}
		}
		return serializationFlags | serializationFlags2;
	}

	public SerializationFlags Pack(ref List<uint> value, List<uint> prevValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
	{
		bool flag = IsKeyframe(frameId);
		bool flag2 = (writeFlags & (SerializationFlags)22) != 0;
		SerializationFlags serializationFlags = SerializationFlags.None;
		int num = bitposition;
		int i = 0;
		for (int count = value.Count; i < count; i++)
		{
			uint num2 = value[i];
			if (!flag)
			{
				if (!flag2 && num2 == prevValue[i])
				{
					buffer.WriteBool(b: false, ref bitposition);
					continue;
				}
				buffer.WriteBool(b: true, ref bitposition);
			}
			buffer.WritePackedBytes(num2, ref bitposition, bitCount);
			serializationFlags |= SerializationFlags.HasContent;
		}
		if (serializationFlags == SerializationFlags.None)
		{
			bitposition = num;
		}
		return serializationFlags;
	}

	public SerializationFlags Unpack(ref List<uint> value, BitArray isCompleteMask, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
	{
		bool flag = IsKeyframe(frameId);
		SerializationFlags serializationFlags = SerializationFlags.None;
		SerializationFlags serializationFlags2 = SerializationFlags.IsComplete;
		int i = 0;
		for (int count = value.Count; i < count; i++)
		{
			if (!flag && !buffer.ReadBool(ref bitposition))
			{
				serializationFlags2 = SerializationFlags.None;
				isCompleteMask[i] = false;
			}
			else
			{
				isCompleteMask[i] = true;
				value[i] = (uint)buffer.ReadPackedBytes(ref bitposition, bitCount);
				serializationFlags |= SerializationFlags.HasContent;
			}
		}
		return serializationFlags | serializationFlags2;
	}

	public static void Copy<T>(List<T> src, List<T> trg, BitArray mask) where T : struct
	{
		int i = 0;
		for (int count = src.Count; i < count; i++)
		{
			if (mask.Get(i))
			{
				trg[i] = src[i];
			}
		}
	}

	public static void Capture<T>(List<T> src, List<T> trg) where T : struct
	{
		int i = 0;
		for (int count = src.Count; i < count; i++)
		{
			trg[i] = src[i];
		}
	}
}
