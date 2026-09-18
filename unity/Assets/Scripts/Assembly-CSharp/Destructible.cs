using System;
using UnityEngine;

public class Destructible : Damagable
{
	public float threshold = 25f;

	public float force = 1f;

	public float rangeMulti = 1f;

	public override void CallTakeDamage(Vector2 damage, Vector2 damagePosition, GameObject damagingWeapon = null, Player damagingPlayer = null, bool lethal = true)
	{
		throw new NotImplementedException();
	}

	public override void TakeDamage(Vector2 damage, Vector2 position, GameObject damagingWeapon = null, Player damagingPlayer = null, bool lethal = true, bool ignoreBlock = false)
	{
		if (!(damage.magnitude < threshold))
		{
			damage = damage.normalized * 100f;
			Transform[] array = new Transform[base.transform.childCount];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = base.transform.GetChild(i);
			}
			foreach (Transform transform in array)
			{
				transform.gameObject.SetActive(true);
				transform.SetParent(null, true);
				float num = damage.magnitude * 0.02f * rangeMulti;
				float num2 = Mathf.Clamp((num - Vector2.Distance(transform.position, position)) / num, 0f, 1f);
				Rigidbody2D component = transform.GetComponent<Rigidbody2D>();
				component.AddForce(damage * num2 * force * 0.1f * component.mass, ForceMode2D.Impulse);
				transform.gameObject.AddComponent<RemoveAfterSeconds>().seconds = UnityEngine.Random.Range(1f, 3f);
			}
			base.gameObject.SetActive(false);
		}
	}

	public override void TakeDamage(Vector2 damage, Vector2 damagePosition, Color dmgColor, GameObject damagingWeapon = null, Player damagingPlayer = null, bool lethal = true, bool ignoreBlock = false)
	{
		TakeDamage(damage, damagePosition, dmgColor, damagingWeapon, damagingPlayer, lethal);
	}
}
