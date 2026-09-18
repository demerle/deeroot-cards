namespace Photon.Compression.Internal;

[PackSupportedTypes(typeof(byte))]
public interface IPackByte
{
	SerializationFlags Pack(ref byte value, byte prevvalue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

	SerializationFlags Unpack(ref byte value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
}
