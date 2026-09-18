using System;
using UnityEngine;

public class BounceTrigger : MonoBehaviour
{
	private BounceEffect[] bounceEffects;

	private void Start()
	{
		bounceEffects = GetComponents<BounceEffect>();
		RayHitReflect componentInParent = GetComponentInParent<RayHitReflect>();
		componentInParent.reflectAction = (Action<HitInfo>)Delegate.Combine(componentInParent.reflectAction, new Action<HitInfo>(Reflect));
	}

	public void Reflect(HitInfo hit)
	{
		for (int i = 0; i < bounceEffects.Length; i++)
		{
			bounceEffects[i].DoBounce(hit);
		}
	}
}
