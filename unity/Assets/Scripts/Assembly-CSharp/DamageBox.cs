using Sonigon;
using UnityEngine;

public class DamageBox : MonoBehaviour
{
	[Header("Sound")]
	public bool soundPlaySawDamage;

	public SoundEvent soundSawDamage;

	[Header("Settings")]
	public bool towardsCenterOfMap;

	public bool awayFromMe;

	public float damage = 25f;

	public float force;

	public float setFlyingFor;

	public float shake;

	public float cd = 0.3f;

	public bool ignoreBlock;

	public ParticleSystem dmgPart;

	private float time;

	private SpawnedAttack spawned;

	private void Start()
	{
		spawned = GetComponentInParent<SpawnedAttack>();
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		Collide(collision);
	}

	private void OnCollisionStay2D(Collision2D collision)
	{
		Collide(collision);
	}

	private void Collide(Collision2D collision)
	{
		if (Time.time < time + cd)
		{
			return;
		}
		Vector3 vector = base.transform.root.forward;
		if (towardsCenterOfMap)
		{
			vector = -collision.contacts[0].point.normalized;
		}
		if (awayFromMe)
		{
			vector = (collision.transform.position - base.transform.position).normalized;
		}
		Damagable componentInParent = collision.transform.GetComponentInParent<Damagable>();
		if (!componentInParent)
		{
			return;
		}
		time = Time.time;
		HealthHandler component = componentInParent.GetComponent<HealthHandler>();
		CharacterData component2 = component.GetComponent<CharacterData>();
		if (!component2 || component2.view.IsMine)
		{
			if ((bool)component)
			{
				component.CallTakeForce(vector * force, ForceMode2D.Impulse, false, ignoreBlock, setFlyingFor);
			}
			componentInParent.CallTakeDamage(damage * vector, base.transform.position, null, (spawned != null) ? spawned.spawner : null);
			if (soundPlaySawDamage)
			{
				SoundManager.Instance.PlayAtPosition(soundSawDamage, SoundManager.Instance.GetTransform(), base.transform);
			}
			if ((bool)dmgPart)
			{
				Vector3 forward = vector;
				vector.z = 0f;
				dmgPart.transform.parent.rotation = Quaternion.LookRotation(forward);
				dmgPart.Play();
			}
			if (shake != 0f)
			{
				component2.player.Call_AllGameFeel(shake * (Vector2)vector);
			}
		}
	}
}
