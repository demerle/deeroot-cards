namespace Photon.Compression.Internal
{
	[PackSupportedTypes(typeof(ushort))]
	public interface IPackUInt16
	{
		SerializationFlags Pack(ref ushort value, ushort preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

		SerializationFlags Unpack(ref ushort value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
	}
}
