using UnityEngine;

namespace Photon.Compression.Internal;

[PackSupportedTypes(typeof(Vector2Int))]
public interface IPackVector2Int
{
	SerializationFlags Pack(ref Vector2Int value, Vector2Int preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

	SerializationFlags Unpack(ref Vector2Int value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
}
