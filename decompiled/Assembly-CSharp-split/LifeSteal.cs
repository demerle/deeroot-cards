using UnityEngine;

public class LifeSteal : DealtDamageEffect
{
	private HealthHandler health;

	public float multiplier;

	public override void DealtDamage(Vector2 damage, bool selfDamage, Player damagedPlayer = null)
	{
		if (!selfDamage)
		{
			if (!health)
			{
				health = GetComponentInParent<HealthHandler>();
			}
			health.Heal(damage.magnitude * multiplier);
		}
	}
}
