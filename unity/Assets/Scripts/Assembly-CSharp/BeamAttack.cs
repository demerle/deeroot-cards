using System.Collections;
using UnityEngine;

public class BeamAttack : MonoBehaviour
{
	public float selfHeal;

	public float damage = 8f;

	public float force = 2500f;

	public float scalingForce;

	public float overTimeForce;

	public float overTimeScalingForce;

	public float overTimeDrag;

	public float effectOverTimeTime = 0.1f;

	public float interval = 0.2f;

	public float slow;

	public float maxSlow = 1f;

	public Color dmgColor;

	private Player attacker;

	private Player thisPlayer;

	private LineEffect[] lineEffects;

	private ParticleSystem[] parts;

	private CharacterStatModifiers stats;

	private float scaleMultiplier = 1f;

	private SpawnedAttack spawnedAttack;

	private float counter;

	private void Start()
	{
		lineEffects = GetComponentsInChildren<LineEffect>(true);
		parts = GetComponentsInChildren<ParticleSystem>();
		thisPlayer = GetComponentInParent<Player>();
		stats = thisPlayer.GetComponent<CharacterStatModifiers>();
		attacker = PlayerManager.instance.GetOtherPlayer(thisPlayer);
		scaleMultiplier = base.transform.localScale.x;
		spawnedAttack = GetComponentInParent<SpawnedAttack>();
		if (thisPlayer == spawnedAttack.spawner)
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void Update()
	{
		if (!attacker || !thisPlayer)
		{
			return;
		}
		counter += TimeHandler.deltaTime;
		if (!(counter > interval))
		{
			return;
		}
		CanSeeInfo canSeeInfo = PlayerManager.instance.CanSeePlayer(attacker.transform.position, thisPlayer);
		if (canSeeInfo.canSee)
		{
			Vector2 vector = thisPlayer.transform.position - attacker.transform.position;
			Vector2 normalized = vector.normalized;
			if (force != 0f)
			{
				thisPlayer.data.healthHandler.TakeForce(normalized * scaleMultiplier * force);
			}
			if (scalingForce != 0f)
			{
				thisPlayer.data.healthHandler.TakeForce(vector * scaleMultiplier * scalingForce);
			}
			if (damage != 0f)
			{
				thisPlayer.data.healthHandler.TakeDamage(damage * scaleMultiplier * normalized, base.transform.position, dmgColor, null, attacker);
			}
			if (selfHeal != 0f)
			{
				attacker.data.healthHandler.Heal(selfHeal * scaleMultiplier);
			}
			for (int i = 0; i < lineEffects.Length; i++)
			{
				lineEffects[i].Play(attacker.transform, thisPlayer.transform);
			}
			StartCoroutine(DoOverTimeEffects(attacker));
			if (slow > 0f && (bool)stats)
			{
				stats.AddSlowAddative(slow * scaleMultiplier, maxSlow);
			}
		}
		else
		{
			for (int j = 0; j < lineEffects.Length; j++)
			{
				lineEffects[j].Play(attacker.transform, canSeeInfo.hitPoint);
			}
			for (int k = 0; k < parts.Length; k++)
			{
				parts[k].transform.position = canSeeInfo.hitPoint;
				parts[k].transform.localScale = Vector3.one * scaleMultiplier;
				parts[k].Play();
			}
		}
		counter = 0f;
	}

	private IEnumerator DoOverTimeEffects(Player attacker)
	{
		float c = 0f;
		while (c < effectOverTimeTime)
		{
			c += TimeHandler.deltaTime;
			if ((bool)attacker && (bool)thisPlayer)
			{
				Vector2 vector = thisPlayer.transform.position - attacker.transform.position;
				Vector2 normalized = vector.normalized;
				if (overTimeForce != 0f)
				{
					thisPlayer.data.healthHandler.TakeForce(normalized * scaleMultiplier * TimeHandler.deltaTime * overTimeForce);
				}
				if (overTimeScalingForce != 0f)
				{
					thisPlayer.data.healthHandler.TakeForce(vector * scaleMultiplier * TimeHandler.deltaTime * overTimeScalingForce, ForceMode2D.Force);
				}
				if (overTimeDrag > 0f)
				{
					thisPlayer.data.playerVel.AddForce(-thisPlayer.data.playerVel.velocity * Mathf.Clamp(TimeHandler.deltaTime * scaleMultiplier * overTimeDrag, 0f, 0.95f), ForceMode2D.Force);
				}
			}
			yield return null;
		}
	}
}
