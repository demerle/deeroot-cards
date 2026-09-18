namespace Photon.Pun.Simple;

public interface IOnInterpolate
{
	bool OnInterpolate(int snapFrameId, int targFrameId, float t);
}
