using UnityEngine;

public class RayHitBash : RayHitEffect
{
	public float triggerChancePerTenDamage = 0.1f;

	public float baseTriggerChance = 0.2f;

	[Space(15f)]
	public float stunMultiplier = 1f;

	public float stunTimePerTenDamage = 0.1f;

	public float baseStunTime = 1f;

	public bool cannotPermaStun;

	[Space(15f)]
	public float stunTimeThreshold = 0.2f;

	public float stunTimeExponent = 1f;

	public float multiplierPerTenMeterTravelled;

	private MoveTransform move;

	private float multiplier = 1f;

	private void Start()
	{
		move = GetComponentInParent<MoveTransform>();
		multiplier = base.transform.localScale.x;
	}

	public override HasToReturn DoHitEffect(HitInfo hit)
	{
		if (!hit.transform)
		{
			return HasToReturn.canContinue;
		}
		StunHandler component = hit.transform.GetComponent<StunHandler>();
		if ((bool)component)
		{
			ProjectileHit componentInParent = GetComponentInParent<ProjectileHit>();
			float num = 25f;
			if ((bool)componentInParent)
			{
				num = componentInParent.damage;
			}
			float num2 = triggerChancePerTenDamage * num * 0.1f;
			num2 += baseTriggerChance;
			if (Random.value < num2)
			{
				float num3 = baseStunTime + stunTimePerTenDamage * num * 0.1f;
				SetMultiplier();
				num3 *= stunMultiplier;
				num3 = Mathf.Pow(num3, stunTimeExponent);
				num3 *= multiplier;
				if (cannotPermaStun)
				{
					num3 = Mathf.Clamp(num3, 0f, GetComponentInParent<SpawnedAttack>().spawner.data.weaponHandler.gun.attackSpeed * GetComponentInParent<SpawnedAttack>().spawner.data.stats.attackSpeedMultiplier + 0.3f);
				}
				if (num3 > stunTimeThreshold)
				{
					component.AddStun(num3);
				}
			}
		}
		return HasToReturn.canContinue;
	}

	private void SetMultiplier()
	{
		float distanceTravelled = move.distanceTravelled;
		if (multiplierPerTenMeterTravelled != 0f)
		{
			stunMultiplier = distanceTravelled * multiplierPerTenMeterTravelled * 0.1f;
		}
	}
}
