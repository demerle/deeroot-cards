using System;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
	public Image hp;

	public Image white;

	private float drag = 25f;

	private float spring = 25f;

	private float hpCur;

	private float hpVel;

	private float hpTarg;

	private float whiteCur;

	private float whiteVel;

	private float whiteTarg;

	private float sinceDamage;

	private CharacterData data;

	private void Start()
	{
		data = GetComponentInParent<CharacterData>();
		CharacterStatModifiers componentInParent = GetComponentInParent<CharacterStatModifiers>();
		componentInParent.WasDealtDamageAction = (Action<Vector2, bool>)Delegate.Combine(componentInParent.WasDealtDamageAction, new Action<Vector2, bool>(TakeDamage));
	}

	private void Update()
	{
		hpTarg = data.health / data.maxHealth;
		sinceDamage += TimeHandler.deltaTime;
		hpVel = FRILerp.Lerp(hpVel, (hpTarg - hpCur) * spring, drag);
		whiteVel = FRILerp.Lerp(whiteVel, (whiteTarg - whiteCur) * spring, drag);
		hpCur += hpVel * TimeHandler.deltaTime;
		whiteCur += whiteVel * TimeHandler.deltaTime;
		hp.fillAmount = hpCur;
		white.fillAmount = whiteCur;
		if (sinceDamage > 0.5f)
		{
			whiteTarg = hpTarg;
		}
	}

	public void TakeDamage(Vector2 dmg, bool selfDmg)
	{
		sinceDamage = 0f;
	}
}
