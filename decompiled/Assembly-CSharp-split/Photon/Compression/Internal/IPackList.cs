using System.Collections;
using System.Collections.Generic;

namespace Photon.Compression.Internal;

[PackSupportedTypes(typeof(List<>))]
public interface IPackList<T> where T : struct
{
	SerializationFlags Pack(ref List<T> value, List<T> prevvalue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

	SerializationFlags Unpack(ref List<T> value, BitArray mask, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
}
