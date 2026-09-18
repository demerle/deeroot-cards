using UnityEngine.Events;

public class ReflectEvent : RayHitEffect
{
	public UnityEvent bounceEvent;

	public override HasToReturn DoHitEffect(HitInfo hit)
	{
		bounceEvent.Invoke();
		return HasToReturn.canContinue;
	}
}
