using Photon.Utilities;

namespace Photon.Compression.Internal;

public abstract class PackFrame
{
	public FastBitMask128 mask;

	public FastBitMask128 isCompleteMask;
}
