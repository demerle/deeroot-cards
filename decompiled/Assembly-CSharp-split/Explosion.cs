using System;
using Photon.Pun;
using Sirenix.OdinInspector;
using Sonigon;
using UnityEngine;

public class Explosion : MonoBehaviour
{
	[Header("Sounds")]
	public SoundEvent soundDamage;

	[Header("Settings")]
	public float slow;

	public float silence;

	public bool fastSlow;

	public float stun;

	public float force = 2000f;

	public float objectForceMultiplier = 1f;

	public bool forceIgnoreMass;

	public float damage = 25f;

	public Color dmgColor = Color.black;

	public float range = 2f;

	public float flyingFor;

	public bool auto = true;

	public bool ignoreTeam;

	public bool ignoreWalls;

	public bool staticRangeMultiplier;

	[FoldoutGroup("Scaling", 0)]
	public bool scaleSlow = true;

	[FoldoutGroup("Scaling", 0)]
	public bool scaleSilence = true;

	[FoldoutGroup("Scaling", 0)]
	public bool scaleDmg = true;

	[FoldoutGroup("Scaling", 0)]
	public bool scaleRadius = true;

	[FoldoutGroup("Scaling", 0)]
	public bool scaleStun = true;

	[FoldoutGroup("Scaling", 0)]
	public bool scaleForce = true;

	[FoldoutGroup("Immunity", 0)]
	public float immunity;

	private SpawnedAttack spawned;

	public bool locallySimulated;

	public Action<Damagable> DealDamageAction;

	public Action<Damagable> DealHealAction;

	public Action<Damagable, float> HitTargetAction;

	private PhotonView view;

	public Action<CharacterData, float> hitPlayerAction;

	private void Start()
	{
		spawned = GetComponent<SpawnedAttack>();
		view = GetComponent<PhotonView>();
		if (auto)
		{
			Explode();
		}
	}

	private void DoExplosionEffects(Collider2D hitCol, float rangeMultiplier, float distance)
	{
		float num = (scaleDmg ? base.transform.localScale.x : 1f);
		float num2 = (scaleForce ? base.transform.localScale.x : 1f);
		float num3 = (scaleSlow ? base.transform.localScale.x : 1f);
		float num4 = (scaleSilence ? base.transform.localScale.x : 1f);
		float num5 = (scaleStun ? ((1f + base.transform.localScale.x) * 0.5f) : 1f);
		Damagable componentInParent = hitCol.gameObject.GetComponentInParent<Damagable>();
		CharacterData characterData = null;
		if ((bool)componentInParent)
		{
			characterData = hitCol.gameObject.GetComponentInParent<CharacterData>();
			if ((immunity > 0f && (bool)characterData && characterData.GetComponent<PlayerImmunity>().IsImune(immunity, num * damage * rangeMultiplier, base.gameObject.name)) || (!ignoreWalls && (bool)characterData && !PlayerManager.instance.CanSeePlayer(base.transform.position, characterData.player).canSee))
			{
				return;
			}
			if (slow != 0f && (bool)componentInParent.GetComponent<CharacterStatModifiers>())
			{
				if (locallySimulated)
				{
					if (spawned.IsMine() && !characterData.block.IsBlocking())
					{
						characterData.stats.RPCA_AddSlow(slow * rangeMultiplier * num3, fastSlow);
					}
				}
				else if (spawned.IsMine() && !characterData.block.IsBlocking())
				{
					characterData.view.RPC("RPCA_AddSlow", RpcTarget.All, slow * rangeMultiplier * num3, fastSlow);
				}
			}
			if (silence != 0f && (bool)componentInParent.GetComponent<SilenceHandler>() && spawned.IsMine() && !characterData.block.IsBlocking())
			{
				characterData.view.RPC("RPCA_AddSilence", RpcTarget.All, silence * rangeMultiplier * num4);
			}
			if ((bool)spawned)
			{
				_ = spawned.spawner;
			}
			hitPlayerAction?.Invoke(characterData, rangeMultiplier);
			if (damage < 0f)
			{
				if ((bool)characterData)
				{
					characterData.healthHandler.Heal(0f - damage);
				}
				if (DealHealAction != null)
				{
					DealHealAction(componentInParent);
				}
			}
			else if (damage > 0f)
			{
				if (soundDamage != null && characterData != null)
				{
					SoundManager.Instance.Play(soundDamage, characterData.transform);
				}
				Vector2 vector = ((Vector2)hitCol.bounds.ClosestPoint(base.transform.position) - (Vector2)base.transform.position).normalized;
				if (vector == Vector2.zero)
				{
					vector = Vector2.up;
				}
				if (spawned.IsMine())
				{
					componentInParent.CallTakeDamage(num * damage * rangeMultiplier * vector, base.transform.position, null, spawned.spawner);
				}
				if (DealDamageAction != null)
				{
					DealDamageAction(componentInParent);
				}
			}
		}
		if ((bool)characterData)
		{
			if (HitTargetAction != null)
			{
				HitTargetAction(componentInParent, distance);
			}
			if (force != 0f)
			{
				if (locallySimulated)
				{
					characterData.healthHandler.TakeForce(((Vector2)hitCol.bounds.ClosestPoint(base.transform.position) - (Vector2)base.transform.position).normalized * rangeMultiplier * force * num2, ForceMode2D.Impulse, forceIgnoreMass);
				}
				else if (spawned.IsMine())
				{
					characterData.healthHandler.CallTakeForce(((Vector2)hitCol.bounds.ClosestPoint(base.transform.position) - (Vector2)base.transform.position).normalized * rangeMultiplier * force * num2, ForceMode2D.Impulse, forceIgnoreMass, ignoreBlock: false, flyingFor * rangeMultiplier);
				}
			}
			if (stun > 0f)
			{
				characterData.stunHandler.AddStun(stun * num5);
			}
		}
		else if ((bool)hitCol.attachedRigidbody)
		{
			hitCol.attachedRigidbody.AddForce(((Vector2)hitCol.bounds.ClosestPoint(base.transform.position) - (Vector2)base.transform.position).normalized * rangeMultiplier * force * num2, ForceMode2D.Impulse);
		}
	}

	public void Explode()
	{
		float num = (scaleRadius ? base.transform.localScale.x : 1f);
		Collider2D[] array = Physics2D.OverlapCircleAll(base.transform.position, range * num);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].gameObject.layer != 19)
			{
				Damagable componentInParent = array[i].gameObject.GetComponentInParent<Damagable>();
				float num2 = Vector2.Distance(base.transform.position, array[i].bounds.ClosestPoint(base.transform.position));
				float value = 1f - num2 / (range * num);
				if (staticRangeMultiplier)
				{
					value = 1f;
				}
				value = Mathf.Clamp(value, 0f, 1f);
				NetworkPhysicsObject component = array[i].GetComponent<NetworkPhysicsObject>();
				if ((bool)component && component.photonView.IsMine)
				{
					float num3 = (scaleForce ? base.transform.localScale.x : 1f);
					component.BulletPush((Vector2)(component.transform.position - base.transform.position).normalized * objectForceMultiplier * 1f * value * force * num3, Vector2.zero, null);
				}
				if (((bool)componentInParent || (bool)array[i].attachedRigidbody) && (!ignoreTeam || !spawned || !(spawned.spawner.gameObject == array[i].transform.gameObject)))
				{
					DoExplosionEffects(array[i], value, num2);
				}
			}
		}
	}
}
