namespace Photon.Compression.Internal;

[PackSupportedTypes(typeof(bool))]
public interface IPackBoolean
{
	SerializationFlags Pack(ref bool value, bool prevvalue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

	SerializationFlags Unpack(ref bool value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
}
