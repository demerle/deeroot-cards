namespace Photon.Compression.Internal
{
	[PackSupportedTypes(typeof(char))]
	public interface IPackChar
	{
		SerializationFlags Pack(ref char value, char prevvalue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

		SerializationFlags Unpack(ref char value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
	}
}
