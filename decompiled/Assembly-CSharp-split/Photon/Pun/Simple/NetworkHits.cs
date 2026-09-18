using System.Collections.Generic;
using Photon.Compression;

namespace Photon.Pun.Simple;

public class NetworkHits
{
	public readonly List<NetworkHit> hits = new List<NetworkHit>();

	public bool nearestOnly;

	public int nearestIndex = -1;

	public int bitsForContactGroupMask;

	public NetworkHits(bool nearestOnly, int bitsForContactGroupMask)
	{
		this.nearestOnly = nearestOnly;
		this.bitsForContactGroupMask = bitsForContactGroupMask;
	}

	public void Reset(bool nearestOnly, int bitsForContactGroupMask)
	{
		this.nearestOnly = nearestOnly;
		this.bitsForContactGroupMask = bitsForContactGroupMask;
		hits.Clear();
		nearestIndex = -1;
	}

	public void Clear()
	{
		hits.Clear();
		nearestIndex = -1;
	}

	public SerializationFlags Serialize(byte[] buffer, ref int bitposition, int bitsForColliderId)
	{
		SerializationFlags result = SerializationFlags.None;
		if (nearestOnly)
		{
			if (nearestIndex != -1)
			{
				buffer.WriteBool(b: true, ref bitposition);
				hits[nearestIndex].Serialize(buffer, ref bitposition, bitsForContactGroupMask, bitsForColliderId);
				result = SerializationFlags.HasContent;
			}
			else
			{
				buffer.WriteBool(b: false, ref bitposition);
			}
		}
		else
		{
			int i = 0;
			_ = hits.Count;
			for (; i < hits.Count; i++)
			{
				buffer.WriteBool(b: true, ref bitposition);
				hits[i].Serialize(buffer, ref bitposition, bitsForContactGroupMask, bitsForColliderId);
				result = SerializationFlags.HasContent;
			}
			buffer.WriteBool(b: false, ref bitposition);
		}
		return result;
	}

	public SerializationFlags Deserialize(byte[] buffer, ref int bitposition, int bitsForColliderId)
	{
		hits.Clear();
		SerializationFlags result = SerializationFlags.None;
		if (nearestOnly)
		{
			if (buffer.ReadBool(ref bitposition))
			{
				hits.Add(NetworkHit.Deserialize(buffer, ref bitposition, bitsForContactGroupMask, bitsForColliderId));
				result = SerializationFlags.HasContent;
				nearestIndex = 0;
			}
			else
			{
				nearestIndex = -1;
			}
		}
		else
		{
			while (buffer.ReadBool(ref bitposition))
			{
				hits.Add(NetworkHit.Deserialize(buffer, ref bitposition, bitsForContactGroupMask, bitsForColliderId));
				result = SerializationFlags.HasContent;
			}
		}
		return result;
	}

	public override string ToString()
	{
		string text = GetType().Name;
		for (int i = 0; i < hits.Count; i++)
		{
			text = text + "\nObj:" + hits[i].netObjId + " Mask:" + hits[i].hitMask;
		}
		return text;
	}
}
