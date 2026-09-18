namespace Photon.Pun.Simple
{
	public interface IOnIncrementFrame
	{
		void OnIncrementFrame(int newFrameId, int newSubFrameId, int previousFrameId, int prevSubFrameId);
	}
}
