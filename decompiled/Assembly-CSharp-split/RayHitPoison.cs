using Sonigon;
using UnityEngine;

public class RayHitPoison : RayHitEffect
{
	[Header("Sounds")]
	public SoundEvent soundEventDamageOverTime;

	[Header("Settings")]
	public float time = 2f;

	public float interval = 0.5f;

	public Color color = Color.red;

	private void Start()
	{
		GetComponentInParent<ProjectileHit>().bulletCanDealDeamage = false;
	}

	public override HasToReturn DoHitEffect(HitInfo hit)
	{
		if (!hit.transform)
		{
			return HasToReturn.canContinue;
		}
		RayHitPoison[] componentsInChildren = base.transform.root.GetComponentsInChildren<RayHitPoison>();
		ProjectileHit componentInParent = GetComponentInParent<ProjectileHit>();
		DamageOverTime component = hit.transform.GetComponent<DamageOverTime>();
		if ((bool)component)
		{
			component.TakeDamageOverTime(componentInParent.damage * base.transform.forward / componentsInChildren.Length, base.transform.position, time, interval, color, soundEventDamageOverTime, GetComponentInParent<ProjectileHit>().ownWeapon, GetComponentInParent<ProjectileHit>().ownPlayer);
		}
		return HasToReturn.canContinue;
	}
}
