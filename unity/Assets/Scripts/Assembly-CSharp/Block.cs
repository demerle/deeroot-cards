using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Sonigon;
using UnityEngine;

public class Block : MonoBehaviour
{
	[Header("Sounds")]
	public SoundEvent soundBlockStart;

	public SoundEvent soundBlockRecharged;

	public SoundEvent soundBlockBlocked;

	public SoundEvent soundBlockStatusEffect;

	[Header("Settings")]
	public List<GameObject> objectsToSpawn = new List<GameObject>();

	public float sinceBlock;

	private GeneralInput input;

	public ParticleSystem particle;

	public ParticleSystem reloadParticle;

	public ParticleSystem blockedPart;

	public float cooldown;

	public float counter = 1000f;

	public float cdMultiplier = 1f;

	public float cdAdd;

	public float forceToAdd;

	public float forceToAddUp;

	public bool autoBlock;

	public bool blockedThisFrame;

	public int additionalBlocks;

	public float healing;

	private float timeBetweenBlocks = 0.2f;

	private CharacterData data;

	private HealthHandler health;

	private bool active = true;

	public Action BlockRechargeAction;

	private bool blockedLastFrame;

	public Action<BlockTrigger.BlockTriggerType> BlockAction;

	public Action<BlockTrigger.BlockTriggerType> BlockActionEarly;

	public Action<BlockTrigger.BlockTriggerType> FirstBlockActionThatDelaysOthers;

	public Action<BlockTrigger.BlockTriggerType> SuperFirstBlockAction;

	public Vector3 blockedAtPos;

	public bool delayOtherActions;

	public ParticleSystem statusBlockPart;

	private float lastStatusBlock;

	public Action<GameObject, Vector3, Vector3> BlockProjectileAction;

	public float Cooldown()
	{
		return (cooldown + cdAdd) * cdMultiplier;
	}

	private void Start()
	{
		input = GetComponent<GeneralInput>();
		data = GetComponent<CharacterData>();
		health = GetComponent<HealthHandler>();
		sinceBlock = 100f;
	}

	private void Update()
	{
		if ((bool)input && data.playerVel.simulated)
		{
			if (!blockedLastFrame)
			{
				blockedThisFrame = false;
			}
			blockedLastFrame = false;
			sinceBlock += TimeHandler.deltaTime;
			counter += TimeHandler.deltaTime;
			if (counter > Cooldown() && !active)
			{
				active = true;
				reloadParticle.Play();
				BlockRechargeAction();
				SoundManager.Instance.Play(soundBlockRecharged, base.transform);
			}
			if (input.shieldWasPressed)
			{
				TryBlock();
			}
		}
	}

	public void DoBlockAtPosition(bool firstBlock, bool dontSetCD = false, BlockTrigger.BlockTriggerType triggerType = BlockTrigger.BlockTriggerType.Default, Vector3 blockPos = default(Vector3), bool onlyBlockEffects = false)
	{
		blockedAtPos = blockPos;
		RPCA_DoBlock(firstBlock, dontSetCD, triggerType, blockPos, onlyBlockEffects);
	}

	internal void ResetStats()
	{
		objectsToSpawn = new List<GameObject>();
		sinceBlock = 10f;
		cooldown = 4f;
		counter = 1000f;
		cdMultiplier = 1f;
		cdAdd = 0f;
		forceToAdd = 0f;
		forceToAddUp = 0f;
		autoBlock = false;
		blockedThisFrame = false;
		additionalBlocks = 0;
		healing = 0f;
		delayOtherActions = false;
	}

	public void CallDoBlock(bool firstBlock, bool dontSetCD = false, BlockTrigger.BlockTriggerType triggerType = BlockTrigger.BlockTriggerType.Default, Vector3 useBlockPos = default(Vector3), bool onlyBlockEffects = false)
	{
		data.view.RPC("RPCA_DoBlock", RpcTarget.All, firstBlock, dontSetCD, (int)triggerType, useBlockPos, onlyBlockEffects);
	}

	[PunRPC]
	public void RPCA_DoBlock(bool firstBlock, bool dontSetCD = false, BlockTrigger.BlockTriggerType triggerType = BlockTrigger.BlockTriggerType.Default, Vector3 useBlockPos = default(Vector3), bool onlyBlockEffects = false)
	{
		if (triggerType == BlockTrigger.BlockTriggerType.Default && firstBlock)
		{
			for (int i = 0; i < additionalBlocks; i++)
			{
				StartCoroutine(DelayBlock(((float)i + 1f) * timeBetweenBlocks));
			}
		}
		StartCoroutine(IDoBlock(firstBlock, dontSetCD, triggerType, useBlockPos, onlyBlockEffects));
	}

	private IEnumerator IDoBlock(bool firstBlock, bool dontSetCD = false, BlockTrigger.BlockTriggerType triggerType = BlockTrigger.BlockTriggerType.Default, Vector3 useBlockPos = default(Vector3), bool onlyBlockEffects = false)
	{
		active = false;
		Vector3 position = base.transform.position;
		if (useBlockPos != Vector3.zero)
		{
			base.transform.position = useBlockPos;
		}
		if (SuperFirstBlockAction != null)
		{
			SuperFirstBlockAction(triggerType);
		}
		if (FirstBlockActionThatDelaysOthers != null)
		{
			FirstBlockActionThatDelaysOthers(triggerType);
		}
		if (useBlockPos != Vector3.zero)
		{
			base.transform.position = position;
		}
		if (!onlyBlockEffects)
		{
			sinceBlock = 0f;
		}
		if (delayOtherActions)
		{
			yield return new WaitForSeconds(0.2f);
		}
		position = base.transform.position;
		if (useBlockPos != Vector3.zero)
		{
			base.transform.position = useBlockPos;
		}
		if (BlockActionEarly != null)
		{
			BlockActionEarly(triggerType);
		}
		if (BlockAction != null)
		{
			BlockAction(triggerType);
		}
		if (firstBlock)
		{
			if (forceToAdd != 0f)
			{
				health.TakeForce(data.hand.transform.forward * forceToAdd * data.playerVel.mass * 0.01f);
			}
			if (forceToAddUp != 0f)
			{
				health.TakeForce(Vector3.up * forceToAddUp * data.playerVel.mass * 0.01f);
			}
		}
		blockedLastFrame = true;
		bool flag = false;
		for (int i = 0; i < data.currentCards.Count; i++)
		{
			if (data.currentCards[i].soundDisableBlockBasic)
			{
				flag = true;
				break;
			}
		}
		if (!flag && triggerType != BlockTrigger.BlockTriggerType.ShieldCharge)
		{
			SoundManager.Instance.Play(soundBlockStart, base.transform);
		}
		if (!onlyBlockEffects)
		{
			particle.Play();
		}
		if (!dontSetCD)
		{
			counter = 0f;
		}
		GamefeelManager.GameFeel(UnityEngine.Random.insideUnitCircle.normalized * 1f);
		if (!onlyBlockEffects)
		{
			sinceBlock = 0f;
		}
		Spawn();
		health.Heal(healing);
		if (useBlockPos != Vector3.zero)
		{
			base.transform.position = position;
		}
	}

	public void ShowStatusEffectBlock()
	{
		if (!(Time.unscaledTime < lastStatusBlock + 0.25f) && data.view.IsMine)
		{
			lastStatusBlock = Time.unscaledTime;
			data.view.RPC("RPCA_ShowStatusEffectBlock", RpcTarget.All);
		}
	}

	[PunRPC]
	public void RPCA_ShowStatusEffectBlock()
	{
		SoundManager.Instance.Play(soundBlockStatusEffect, base.transform);
		statusBlockPart.Play();
	}

	public void TryBlock()
	{
		if (!(counter < Cooldown()))
		{
			RPCA_DoBlock(true);
			counter = 0f;
		}
	}

	private IEnumerator DelayBlock(float t)
	{
		yield return new WaitForSeconds(t);
		yield return new WaitForEndOfFrame();
		RPCA_DoBlock(false, true, BlockTrigger.BlockTriggerType.Echo);
	}

	public void Spawn()
	{
		for (int i = 0; i < objectsToSpawn.Count; i++)
		{
			SpawnedAttack component = UnityEngine.Object.Instantiate(objectsToSpawn[i], base.transform.position, Quaternion.identity).GetComponent<SpawnedAttack>();
			if ((bool)component)
			{
				component.spawner = GetComponent<Player>();
			}
		}
	}

	public void blocked(GameObject projectile, Vector3 forward, Vector3 hitPos)
	{
		SoundManager.Instance.Play(soundBlockBlocked, base.transform);
		projectile.GetComponent<ProjectileHit>().RemoveOwnPlayerFromPlayersHit();
		projectile.GetComponent<ProjectileHit>().AddPlayerToHeld(GetComponent<HealthHandler>());
		projectile.GetComponent<MoveTransform>().velocity *= -1f;
		projectile.GetComponent<RayCastTrail>().WasBlocked();
		blockedPart.transform.position = hitPos + base.transform.forward * 5f;
		blockedPart.transform.rotation = Quaternion.LookRotation(-forward * 1.5f);
		GamefeelManager.GameFeel(forward);
		blockedPart.Play();
		SpawnedAttack componentInParent = projectile.GetComponentInParent<SpawnedAttack>();
		if ((!componentInParent || !(componentInParent.spawner.gameObject == base.transform.root.gameObject)) && BlockProjectileAction != null)
		{
			BlockProjectileAction(projectile, forward, hitPos);
		}
	}

	public void ResetCD(bool soundPlay)
	{
		active = true;
		reloadParticle.Play();
		counter = Cooldown() + 1f;
		if (soundPlay)
		{
			SoundManager.Instance.Play(soundBlockRecharged, base.transform);
		}
	}

	public bool TryBlockMe(GameObject toBlock, Vector3 forward, Vector3 hitPos)
	{
		if (sinceBlock < 0.3f)
		{
			blocked(toBlock, forward, hitPos);
			sinceBlock = 0f;
			particle.Play();
			return true;
		}
		return false;
	}

	public void DoBlock(GameObject toBlock, Vector3 forward, Vector3 hitPos)
	{
		sinceBlock = 0f;
		blocked(toBlock, forward, hitPos);
		particle.Play();
	}

	public bool IsBlocking()
	{
		if (sinceBlock < 0.3f)
		{
			ShowStatusEffectBlock();
		}
		return sinceBlock < 0.3f;
	}

	public bool IsOnCD()
	{
		return counter < Cooldown();
	}
}
