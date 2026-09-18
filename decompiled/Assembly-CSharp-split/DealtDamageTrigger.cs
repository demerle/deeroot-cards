using System;
using UnityEngine;
using UnityEngine.Events;

public class DealtDamageTrigger : MonoBehaviour
{
	public UnityEvent triggerEvent;

	private void Start()
	{
		CharacterStatModifiers stats = GetComponentInParent<Player>().data.stats;
		stats.DealtDamageAction = (Action<Vector2, bool>)Delegate.Combine(stats.DealtDamageAction, new Action<Vector2, bool>(DealtDamage));
	}

	private void DealtDamage(Vector2 dmg, bool lethal)
	{
		triggerEvent.Invoke();
	}
}
