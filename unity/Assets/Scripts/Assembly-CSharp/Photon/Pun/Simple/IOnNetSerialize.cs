using Photon.Compression;

namespace Photon.Pun.Simple
{
	public interface IOnNetSerialize
	{
		bool SkipWhenEmpty { get; }

		SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags);

		SerializationFlags OnNetDeserialize(int originFrameId, byte[] buffer, ref int bitposition, FrameArrival frameArrival);
	}
}
