using System;
using System.Collections;
using System.Collections.Generic;
using Sonigon;
using UnityEngine;

public class ShieldCharge : MonoBehaviour
{
	[Header("Sounds")]
	public SoundEvent soundShieldCharge;

	[Header("Settings")]
	public float damagePerLevel;

	[Header("Settings")]
	public float knockBackPerLevel;

	public float forcePerLevel;

	public float timePerLevel;

	public ParticleSystem hitPart;

	public float shake;

	public float damage;

	public float knockBack;

	public float stopForce;

	public AnimationCurve forceCurve;

	public float force;

	public float drag;

	public float time;

	private CharacterData data;

	private AttackLevel level;

	private Vector3 dir;

	private bool cancelForce;

	private List<CharacterData> hitDatas = new List<CharacterData>();

	private float blockTime;

	private void Start()
	{
		level = GetComponent<AttackLevel>();
		data = GetComponentInParent<CharacterData>();
		PlayerCollision component = data.GetComponent<PlayerCollision>();
		component.collideWithPlayerAction = (Action<Vector2, Vector2, Player>)Delegate.Combine(component.collideWithPlayerAction, new Action<Vector2, Vector2, Player>(Collide));
		GetComponentInParent<ChildRPC>().childRPCsVector2Vector2Int.Add("ShieldChargeCollide", RPCA_Collide);
		Block componentInParent = GetComponentInParent<Block>();
		componentInParent.SuperFirstBlockAction = (Action<BlockTrigger.BlockTriggerType>)Delegate.Combine(componentInParent.SuperFirstBlockAction, new Action<BlockTrigger.BlockTriggerType>(DoBlock));
	}

	private void OnDestroy()
	{
		PlayerCollision component = data.GetComponent<PlayerCollision>();
		component.collideWithPlayerAction = (Action<Vector2, Vector2, Player>)Delegate.Remove(component.collideWithPlayerAction, new Action<Vector2, Vector2, Player>(Collide));
		GetComponentInParent<ChildRPC>().childRPCsVector2Vector2Int.Remove("ShieldChargeCollide");
		Block componentInParent = GetComponentInParent<Block>();
		componentInParent.SuperFirstBlockAction = (Action<BlockTrigger.BlockTriggerType>)Delegate.Remove(componentInParent.SuperFirstBlockAction, new Action<BlockTrigger.BlockTriggerType>(DoBlock));
	}

	private void Update()
	{
		blockTime -= TimeHandler.deltaTime;
	}

	public void DoBlock(BlockTrigger.BlockTriggerType trigger)
	{
		if (trigger != BlockTrigger.BlockTriggerType.ShieldCharge)
		{
			Charge(trigger);
		}
	}

	public void Charge(BlockTrigger.BlockTriggerType trigger)
	{
		StartCoroutine(DoCharge(trigger));
	}

	private IEnumerator DoCharge(BlockTrigger.BlockTriggerType trigger)
	{
		SoundManager.Instance.Play(soundShieldCharge, base.transform);
		cancelForce = false;
		hitDatas.Clear();
		if (trigger == BlockTrigger.BlockTriggerType.Empower)
		{
			Vector3 currentPos = base.transform.position;
			yield return new WaitForSeconds(0f);
			dir = (currentPos - base.transform.position).normalized;
		}
		else
		{
			dir = data.aimDirection;
		}
		float usedTime = (blockTime = time + (float)level.LevelsUp() * timePerLevel);
		float num = time * 0.1f + (float)level.LevelsUp() * time * 0.15f;
		for (int i = 0; i < level.LevelsUp(); i++)
		{
			float num2 = time / (float)level.attackLevel;
			float time2 = time;
			num += num2;
			StartCoroutine(DelayBlock(num));
		}
		float c = 0f;
		while (c < 1f)
		{
			c += Time.fixedDeltaTime / usedTime;
			if (!cancelForce)
			{
				data.healthHandler.TakeForce(dir * forceCurve.Evaluate(c) * (force + (float)level.LevelsUp() * forcePerLevel), ForceMode2D.Force, true, true);
				data.healthHandler.TakeForce(-data.playerVel.velocity * drag * Time.fixedDeltaTime, ForceMode2D.Force, true, true);
			}
			data.sinceGrounded = 0f;
			yield return new WaitForFixedUpdate();
		}
		data.block.RPCA_DoBlock(false, true, BlockTrigger.BlockTriggerType.ShieldCharge);
	}

	private IEnumerator DelayBlock(float delay)
	{
		yield return new WaitForSeconds(delay);
		data.block.RPCA_DoBlock(false, true, BlockTrigger.BlockTriggerType.ShieldCharge);
	}

	public void RPCA_Collide(Vector2 pos, Vector2 colDir, int playerID)
	{
		CharacterData componentInParent = PlayerManager.instance.GetPlayerWithID(playerID).gameObject.GetComponentInParent<CharacterData>();
		if ((bool)componentInParent)
		{
			cancelForce = true;
			hitPart.transform.rotation = Quaternion.LookRotation(dir);
			hitPart.Play();
			componentInParent.healthHandler.TakeDamage(dir * (damage + (float)level.LevelsUp() * damagePerLevel), base.transform.position, null, data.player);
			componentInParent.healthHandler.TakeForce(dir * (knockBack + (float)level.LevelsUp() * knockBackPerLevel));
			data.healthHandler.TakeForce(-dir * knockBack, ForceMode2D.Impulse, false, true);
			data.healthHandler.TakeForce(-dir * stopForce, ForceMode2D.Impulse, true, true);
			data.block.RPCA_DoBlock(false, true, BlockTrigger.BlockTriggerType.ShieldCharge);
			GamefeelManager.GameFeel(dir * shake);
		}
	}

	public void Collide(Vector2 pos, Vector2 colDir, Player player)
	{
		if (!data.view.IsMine || blockTime < 0f)
		{
			return;
		}
		CharacterData componentInParent = player.gameObject.GetComponentInParent<CharacterData>();
		if (!hitDatas.Contains(componentInParent))
		{
			hitDatas.Add(componentInParent);
			if ((bool)componentInParent)
			{
				GetComponentInParent<ChildRPC>().CallFunction("ShieldChargeCollide", pos, colDir, player.playerID);
			}
		}
	}
}
