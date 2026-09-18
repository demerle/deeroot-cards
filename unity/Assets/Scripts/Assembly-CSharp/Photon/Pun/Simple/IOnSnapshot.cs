namespace Photon.Pun.Simple
{
	public interface IOnSnapshot
	{
		bool OnSnapshot(int pre1FrameId, int snapFrameId, int targFrameId, bool prevIsValid, bool snapIsValid, bool targIsValid);
	}
}
