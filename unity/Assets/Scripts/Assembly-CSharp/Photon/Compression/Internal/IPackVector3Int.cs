using UnityEngine;

namespace Photon.Compression.Internal
{
	[PackSupportedTypes(typeof(Vector3Int))]
	public interface IPackVector3Int
	{
		SerializationFlags Pack(ref Vector3Int value, Vector3Int preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

		SerializationFlags Unpack(ref Vector3Int value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
	}
}
