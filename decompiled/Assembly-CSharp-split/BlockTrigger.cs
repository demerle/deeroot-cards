using System;
using UnityEngine;
using UnityEngine.Events;

public class BlockTrigger : MonoBehaviour
{
	public enum BlockTriggerType
	{
		Default,
		None,
		ShieldCharge,
		Echo,
		Empower
	}

	public UnityEvent triggerEvent;

	public UnityEvent triggerEventEarly;

	public bool delayOtherActions;

	public UnityEvent triggerFirstBlockThatDelaysOthers;

	public UnityEvent triggerSuperFirstBlock;

	public UnityEvent successfulBlockEvent;

	public UnityEvent blockRechargeEvent;

	private BlockEffect[] effects;

	public float cooldown;

	private float lastTriggerTime = -5f;

	public BlockTriggerType blackListedType = BlockTriggerType.None;

	public float cooldownSuccess;

	private float lastTriggerTimeSuccessful = -5f;

	private void Start()
	{
		effects = GetComponents<BlockEffect>();
		Block componentInParent = GetComponentInParent<Block>();
		componentInParent.SuperFirstBlockAction = (Action<BlockTriggerType>)Delegate.Combine(componentInParent.SuperFirstBlockAction, new Action<BlockTriggerType>(DoSuperFirstBlock));
		componentInParent.FirstBlockActionThatDelaysOthers = (Action<BlockTriggerType>)Delegate.Combine(componentInParent.FirstBlockActionThatDelaysOthers, new Action<BlockTriggerType>(DoFirstBlockThatDelaysOthers));
		componentInParent.BlockAction = (Action<BlockTriggerType>)Delegate.Combine(componentInParent.BlockAction, new Action<BlockTriggerType>(DoBlock));
		componentInParent.BlockActionEarly = (Action<BlockTriggerType>)Delegate.Combine(componentInParent.BlockActionEarly, new Action<BlockTriggerType>(DoBlockEarly));
		componentInParent.BlockProjectileAction = (Action<GameObject, Vector3, Vector3>)Delegate.Combine(componentInParent.BlockProjectileAction, new Action<GameObject, Vector3, Vector3>(DoBlockedProjectile));
		componentInParent.BlockRechargeAction = (Action)Delegate.Combine(componentInParent.BlockRechargeAction, new Action(DoBlockRecharge));
		if (delayOtherActions)
		{
			GetComponentInParent<Block>().delayOtherActions = true;
		}
	}

	private void OnDestroy()
	{
		Block componentInParent = GetComponentInParent<Block>();
		if ((bool)componentInParent && componentInParent.SuperFirstBlockAction != null)
		{
			componentInParent.SuperFirstBlockAction = (Action<BlockTriggerType>)Delegate.Remove(componentInParent.SuperFirstBlockAction, new Action<BlockTriggerType>(DoSuperFirstBlock));
		}
		if ((bool)componentInParent && componentInParent.FirstBlockActionThatDelaysOthers != null)
		{
			componentInParent.FirstBlockActionThatDelaysOthers = (Action<BlockTriggerType>)Delegate.Remove(componentInParent.FirstBlockActionThatDelaysOthers, new Action<BlockTriggerType>(DoFirstBlockThatDelaysOthers));
		}
		if ((bool)componentInParent && componentInParent.BlockAction != null)
		{
			componentInParent.BlockAction = (Action<BlockTriggerType>)Delegate.Remove(componentInParent.BlockAction, new Action<BlockTriggerType>(DoBlock));
		}
		if ((bool)componentInParent && componentInParent.BlockActionEarly != null)
		{
			componentInParent.BlockActionEarly = (Action<BlockTriggerType>)Delegate.Remove(componentInParent.BlockActionEarly, new Action<BlockTriggerType>(DoBlockEarly));
		}
		if ((bool)componentInParent && componentInParent.BlockProjectileAction != null)
		{
			componentInParent.BlockProjectileAction = (Action<GameObject, Vector3, Vector3>)Delegate.Remove(componentInParent.BlockProjectileAction, new Action<GameObject, Vector3, Vector3>(DoBlockedProjectile));
		}
		if ((bool)componentInParent && componentInParent.BlockRechargeAction != null)
		{
			componentInParent.BlockRechargeAction = (Action)Delegate.Remove(componentInParent.BlockRechargeAction, new Action(DoBlockRecharge));
		}
	}

	public void DoSuperFirstBlock(BlockTriggerType triggerType)
	{
		if (triggerType != blackListedType && !(lastTriggerTime + cooldown > Time.time))
		{
			lastTriggerTime = Time.time;
			triggerSuperFirstBlock.Invoke();
		}
	}

	public void DoFirstBlockThatDelaysOthers(BlockTriggerType triggerType)
	{
		if (triggerType != blackListedType && !(lastTriggerTime + cooldown > Time.time))
		{
			lastTriggerTime = Time.time;
			triggerFirstBlockThatDelaysOthers.Invoke();
		}
	}

	public void DoBlockEarly(BlockTriggerType triggerType)
	{
		if (triggerType != blackListedType && !(lastTriggerTime + cooldown > Time.time))
		{
			lastTriggerTime = Time.time;
			triggerEventEarly.Invoke();
		}
	}

	public void DoBlock(BlockTriggerType triggerType)
	{
		if (triggerType != blackListedType && !(lastTriggerTime + cooldown > Time.time))
		{
			lastTriggerTime = Time.time;
			triggerEvent.Invoke();
		}
	}

	public void DoBlockedProjectile(GameObject projectile, Vector3 forward, Vector3 hitPos)
	{
		if (!(lastTriggerTimeSuccessful + cooldownSuccess > Time.time))
		{
			lastTriggerTimeSuccessful = Time.time;
			successfulBlockEvent.Invoke();
			for (int i = 0; i < effects.Length; i++)
			{
				effects[i].DoBlockedProjectile(projectile, forward, hitPos);
			}
		}
	}

	public void DoBlockRecharge()
	{
		blockRechargeEvent.Invoke();
	}
}
