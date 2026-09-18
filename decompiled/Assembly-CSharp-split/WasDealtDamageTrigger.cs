using UnityEngine;
using UnityEngine.Events;

public class WasDealtDamageTrigger : WasDealtDamageEffect
{
	public float damageNeeded = 25f;

	public float cd = 0.2f;

	public bool allowSelfDamage;

	private float time;

	private float damageDealt;

	public UnityEvent triggerEvent;

	private void Start()
	{
	}

	public override void WasDealtDamage(Vector2 damage, bool selfDamage)
	{
		if (!selfDamage || allowSelfDamage)
		{
			damageDealt += damage.magnitude;
			if (damageDealt > damageNeeded && Time.time > time + cd)
			{
				time = Time.time;
				damageDealt = 0f;
				triggerEvent.Invoke();
			}
		}
	}
}
