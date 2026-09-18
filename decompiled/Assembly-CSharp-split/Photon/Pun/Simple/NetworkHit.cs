using Photon.Compression;

namespace Photon.Pun.Simple;

public struct NetworkHit
{
	public readonly int netObjId;

	public readonly int hitMask;

	public readonly int colliderId;

	public NetworkHit(int objectID, int hitMask, int colliderId)
	{
		netObjId = objectID;
		this.hitMask = hitMask;
		this.colliderId = colliderId;
	}

	public void Serialize(byte[] buffer, ref int bitposition, int bitsForHitmask, int bitsForColliderId)
	{
		buffer.WritePackedBytes((ulong)netObjId, ref bitposition, 32);
		buffer.Write((ulong)hitMask, ref bitposition, bitsForHitmask);
		buffer.Write((ulong)colliderId, ref bitposition, bitsForColliderId);
	}

	public static NetworkHit Deserialize(byte[] buffer, ref int bitposition, int bitsForHitmask, int bitsForColliderId)
	{
		int objectID = (int)buffer.ReadPackedBytes(ref bitposition, 32);
		int num = (int)buffer.Read(ref bitposition, bitsForHitmask);
		int num2 = (int)buffer.Read(ref bitposition, bitsForColliderId);
		return new NetworkHit(objectID, num, num2);
	}
}
