using System.Text;

namespace Photon.Compression.Internal;

[PackSupportedTypes(typeof(StringBuilder))]
public interface IPackStringBuilder
{
	SerializationFlags Pack(ref StringBuilder value, StringBuilder preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);

	SerializationFlags Unpack(ref StringBuilder value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
}
