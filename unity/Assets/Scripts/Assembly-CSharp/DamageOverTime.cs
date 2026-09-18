using System.Collections;
using Sonigon;
using UnityEngine;

public class DamageOverTime : MonoBehaviour
{
	private HealthHandler health;

	private CharacterData data;

	private void Start()
	{
		health = GetComponent<HealthHandler>();
		data = GetComponent<CharacterData>();
	}

	public void TakeDamageOverTime(Vector2 damage, Vector2 position, float time, float interval, Color color, SoundEvent soundDamageOverTime, GameObject damagingWeapon = null, Player damagingPlayer = null, bool lethal = true)
	{
		StartCoroutine(DoDamageOverTime(damage, position, time, interval, color, soundDamageOverTime, damagingWeapon, damagingPlayer, lethal));
	}

	private IEnumerator DoDamageOverTime(Vector2 damage, Vector2 position, float time, float interval, Color color, SoundEvent soundDamageOverTime, GameObject damagingWeapon = null, Player damagingPlayer = null, bool lethal = true)
	{
		float damageDealt = 0f;
		float damageToDeal = damage.magnitude;
		float dpt = damageToDeal / time * interval;
		while (damageDealt < damageToDeal)
		{
			if (soundDamageOverTime != null && data.isPlaying && !data.dead)
			{
				SoundManager.Instance.Play(soundDamageOverTime, base.transform);
			}
			damageDealt += dpt;
			health.DoDamage(damage.normalized * dpt, position, color, damagingWeapon, damagingPlayer, true, lethal);
			yield return new WaitForSeconds(interval / TimeHandler.timeScale);
		}
	}
}
