namespace Photon.Pun.Simple
{
	public interface IDeltaFrameChangeDetect : IUseKeyframes
	{
		bool UseDeltas { get; set; }
	}
}
