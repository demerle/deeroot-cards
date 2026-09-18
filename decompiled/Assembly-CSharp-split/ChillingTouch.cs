using UnityEngine;

public class ChillingTouch : DamageEffect
{
	public float baseSlow = 0.2f;

	public float scalingSlow = 0.01f;

	private AttackLevel level;

	private void Start()
	{
		level = GetComponent<AttackLevel>();
	}

	public override void DoDamageEffect(Vector2 dmg, bool selfDmg, Player damagedPlayer = null)
	{
		damagedPlayer.data.stats.RPCA_AddSlow((baseSlow + dmg.magnitude * scalingSlow) * (1f + ((float)level.attackLevel - 1f) * 0.3f));
	}
}
