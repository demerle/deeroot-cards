namespace Photon.Compression.Internal
{
	[PackSupportedTypes(typeof(short))]
	public interface IPackInt16
	{
		SerializationFlags Pack(ref short value, short preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

		SerializationFlags Unpack(ref short value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
	}
}
