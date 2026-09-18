using System;
using System.Collections;
using UnityEngine;

public class LineEffectExplosionModifier : MonoBehaviour
{
	public AnimationCurve curve;

	public float speed = 1f;

	private LineEffect effect;

	private Coroutine corutine;

	private void Start()
	{
		effect = GetComponent<LineEffect>();
		Explosion componentInParent = GetComponentInParent<Explosion>();
		componentInParent.DealDamageAction = (Action<Damagable>)Delegate.Combine(componentInParent.DealDamageAction, new Action<Damagable>(DealDamage));
		Explosion componentInParent2 = GetComponentInParent<Explosion>();
		componentInParent2.DealHealAction = (Action<Damagable>)Delegate.Combine(componentInParent2.DealHealAction, new Action<Damagable>(DealDamage));
	}

	public void DealDamage(Damagable damagable)
	{
		if (corutine != null)
		{
			StopCoroutine(corutine);
		}
		corutine = StartCoroutine(DoCurve());
	}

	private IEnumerator DoCurve()
	{
		float c = 0f;
		float t = curve.keys[curve.keys.Length - 1].time;
		while (c < t)
		{
			effect.offsetMultiplier = curve.Evaluate(c);
			c += TimeHandler.deltaTime * speed;
			yield return null;
		}
	}
}
