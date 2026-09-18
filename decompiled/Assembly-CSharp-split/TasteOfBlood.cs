using System;
using UnityEngine;

public class TasteOfBlood : MonoBehaviour
{
	public AnimationCurve attackSpeedCurve;

	public float decaySpeed = 1f;

	private float damageValue;

	private CharacterStatModifiers stats;

	private ParticleSystem part;

	private CharacterData data;

	private bool isOn;

	private void Start()
	{
		part = GetComponentInChildren<ParticleSystem>();
		stats = GetComponentInParent<CharacterStatModifiers>();
		CharacterStatModifiers characterStatModifiers = stats;
		characterStatModifiers.DealtDamageAction = (Action<Vector2, bool>)Delegate.Combine(characterStatModifiers.DealtDamageAction, new Action<Vector2, bool>(DealtDamage));
		data = GetComponentInParent<CharacterData>();
	}

	public void DealtDamage(Vector2 damage, bool selfDamage)
	{
		if (!selfDamage)
		{
			damageValue += damage.magnitude;
		}
		damageValue = Mathf.Clamp(damageValue, 0f, 50f);
	}

	private void Update()
	{
		if (!data.isPlaying)
		{
			damageValue = 0f;
		}
		if (damageValue > 0f)
		{
			damageValue -= TimeHandler.deltaTime * decaySpeed;
			isOn = true;
		}
		else
		{
			isOn = false;
		}
		if (damageValue > 10f)
		{
			if (!part.isPlaying)
			{
				part.Play();
			}
		}
		else if (part.isPlaying)
		{
			part.Stop();
		}
		stats.tasteOfBloodSpeed = attackSpeedCurve.Evaluate(damageValue);
	}
}
