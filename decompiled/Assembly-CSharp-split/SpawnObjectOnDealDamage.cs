using UnityEngine;
using UnityEngine.Events;

public class SpawnObjectOnDealDamage : DealtDamageEffect
{
	public UnityEvent triggerEvent;

	public float damageNeeded = 25f;

	public float cd = 0.2f;

	private float time;

	private float damageDealt;

	private SpawnObjectEffect spawn;

	private DamageEffect dmgEffect;

	public bool allowSelfDmg;

	private void Start()
	{
		spawn = GetComponent<SpawnObjectEffect>();
		dmgEffect = GetComponent<DamageEffect>();
	}

	public override void DealtDamage(Vector2 damage, bool selfDamage, Player damagedPlayer = null)
	{
		if (selfDamage && !allowSelfDmg)
		{
			return;
		}
		damageDealt += damage.magnitude;
		if (damageDealt > damageNeeded && Time.time > time + cd)
		{
			time = Time.time;
			damageDealt = 0f;
			if ((bool)spawn)
			{
				spawn.DoEffect(damage);
			}
			if ((bool)dmgEffect)
			{
				dmgEffect.DoDamageEffect(damage, selfDamage, damagedPlayer);
			}
			triggerEvent.Invoke();
		}
	}
}
