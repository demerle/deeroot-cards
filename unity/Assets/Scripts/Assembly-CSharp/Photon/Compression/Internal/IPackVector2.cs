using UnityEngine;

namespace Photon.Compression.Internal
{
	[PackSupportedTypes(typeof(Vector2))]
	public interface IPackVector2
	{
		SerializationFlags Pack(ref Vector2 value, Vector2 preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

		SerializationFlags Unpack(ref Vector2 value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
	}
}
