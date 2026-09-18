using UnityEngine;

namespace Photon.Compression.Internal
{
	[PackSupportedTypes(typeof(Vector3))]
	public interface IPackVector3
	{
		SerializationFlags Pack(ref Vector3 value, Vector3 preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

		SerializationFlags Unpack(ref Vector3 value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
	}
}
