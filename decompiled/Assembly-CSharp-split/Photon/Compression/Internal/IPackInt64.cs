namespace Photon.Compression.Internal;

[PackSupportedTypes(typeof(long))]
public interface IPackInt64
{
	SerializationFlags Pack(ref long value, long preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

	SerializationFlags Unpack(ref long value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
}
