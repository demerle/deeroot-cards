namespace Photon.Pun.Simple;

public struct AnimPassThru
{
	public PassThruType passThruType;

	public int hash;

	public float normlTime;

	public float fixedTime;

	public float duration;

	public int layer;

	public LocalApplyTiming localApplyTiming;

	public AnimPassThru(PassThruType triggerType, int hash, int layer, float normTime, float otherTime, float duration, LocalApplyTiming localApplyTiming = LocalApplyTiming.OnSend)
	{
		passThruType = triggerType;
		this.hash = hash;
		normlTime = normTime;
		fixedTime = otherTime;
		this.duration = duration;
		this.layer = layer;
		this.localApplyTiming = localApplyTiming;
	}
}
