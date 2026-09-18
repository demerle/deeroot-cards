namespace Photon.Pun.Simple;

public interface IOnPostSimulate
{
	void OnPostSimulate(int frameId, int subFrameId, bool isNetTick);
}
