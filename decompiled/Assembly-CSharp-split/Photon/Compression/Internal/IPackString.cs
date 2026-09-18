namespace Photon.Compression.Internal;

[PackSupportedTypes(typeof(string))]
public interface IPackString
{
	SerializationFlags Pack(ref string value, string preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

	SerializationFlags Unpack(ref string value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
}
