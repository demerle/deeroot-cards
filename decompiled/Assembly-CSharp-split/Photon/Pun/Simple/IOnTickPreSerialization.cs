using Photon.Compression;

namespace Photon.Pun.Simple;

public interface IOnTickPreSerialization
{
	SerializationFlags OnPreSerializeTick(int frameId, byte[] buffer, ref int bitposition);
}
