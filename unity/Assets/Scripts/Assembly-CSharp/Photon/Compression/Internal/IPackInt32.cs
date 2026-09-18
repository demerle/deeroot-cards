namespace Photon.Compression.Internal
{
	[PackSupportedTypes(typeof(int))]
	public interface IPackInt32
	{
		SerializationFlags Pack(ref int value, int preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

		SerializationFlags Unpack(ref int value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
	}
}
