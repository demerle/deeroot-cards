using Photon.Utilities;

namespace Photon.Compression;

public interface IPackObjOnReadyChange
{
	void OnPackObjReadyChange(FastBitMask128 readyMask, bool AllAreReady);
}
