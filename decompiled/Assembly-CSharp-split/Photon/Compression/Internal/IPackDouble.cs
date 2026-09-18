namespace Photon.Compression.Internal;

[PackSupportedTypes(typeof(double))]
public interface IPackDouble
{
	SerializationFlags Pack(ref double value, double preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

	SerializationFlags Unpack(ref double value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
}
