using System;
using System.Collections;
using UnityEngine;

public class ParticleExplosionModifier : MonoBehaviour
{
	public AnimationCurve curve;

	public float speed = 1f;

	private ParticleSystem effect;

	private ParticleSystem.MainModule main;

	private Coroutine corutine;

	private void Start()
	{
		effect = GetComponent<ParticleSystem>();
		main = effect.main;
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
			ParticleSystem.MinMaxCurve startSize = main.startSize;
			startSize.constantMin = curve.Evaluate(c) * 0.5f;
			startSize.constantMax = curve.Evaluate(c);
			main.startSize = startSize;
			c += TimeHandler.deltaTime * speed;
			yield return null;
		}
	}
}
