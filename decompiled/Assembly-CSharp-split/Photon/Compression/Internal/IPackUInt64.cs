namespace Photon.Compression.Internal;

[PackSupportedTypes(typeof(ulong))]
public interface IPackUInt64
{
	SerializationFlags Pack(ref ulong value, ulong preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

	SerializationFlags Unpack(ref ulong value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
}
