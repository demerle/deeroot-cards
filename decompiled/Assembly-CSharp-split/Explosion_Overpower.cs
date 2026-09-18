using System;
using UnityEngine;

public class Explosion_Overpower : MonoBehaviour
{
	public float dmgPer100Hp;

	private void Awake()
	{
		Explosion component = GetComponent<Explosion>();
		component.hitPlayerAction = (Action<CharacterData, float>)Delegate.Combine(component.hitPlayerAction, new Action<CharacterData, float>(HitPlayer));
	}

	private void HitPlayer(CharacterData data, float rangeMultiplier)
	{
		SpawnedAttack component = GetComponent<SpawnedAttack>();
		if (component.IsMine())
		{
			float num = component.spawner.data.maxHealth * dmgPer100Hp * 0.01f * base.transform.localScale.x;
			data.healthHandler.CallTakeDamage(num * (data.transform.position - component.spawner.transform.position).normalized, base.transform.position, null, GetComponent<SpawnedAttack>().spawner);
		}
	}
}
