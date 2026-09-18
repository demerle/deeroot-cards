namespace Photon.Compression.Internal
{
	[PackSupportedTypes(typeof(sbyte))]
	public interface IPackSByte
	{
		SerializationFlags Pack(ref sbyte value, sbyte prevvalue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

		SerializationFlags Unpack(ref sbyte value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
	}
}
