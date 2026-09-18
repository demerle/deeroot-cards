namespace Photon.Compression.Internal
{
	[PackSupportedTypes(typeof(float))]
	public interface IPackSingle
	{
		SerializationFlags Pack(ref float value, float preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

		SerializationFlags Unpack(ref float value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
	}
}
