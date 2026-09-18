namespace Photon.Compression.Internal;

[PackSupportedTypes(typeof(uint))]
public interface IPackUInt32
{
	SerializationFlags Pack(ref uint value, uint preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

	SerializationFlags Unpack(ref uint value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
}
