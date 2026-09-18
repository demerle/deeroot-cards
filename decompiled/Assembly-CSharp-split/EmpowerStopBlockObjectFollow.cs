using System;
using UnityEngine;

public class EmpowerStopBlockObjectFollow : MonoBehaviour
{
	private void Start()
	{
		Block componentInParent = GetComponentInParent<Block>();
		componentInParent.BlockAction = (Action<BlockTrigger.BlockTriggerType>)Delegate.Combine(componentInParent.BlockAction, new Action<BlockTrigger.BlockTriggerType>(Block));
	}

	private void OnDestroy()
	{
		Block componentInParent = GetComponentInParent<Block>();
		componentInParent.BlockAction = (Action<BlockTrigger.BlockTriggerType>)Delegate.Remove(componentInParent.BlockAction, new Action<BlockTrigger.BlockTriggerType>(Block));
	}

	private void Block(BlockTrigger.BlockTriggerType triggerTyp)
	{
		if (triggerTyp == BlockTrigger.BlockTriggerType.Empower && (bool)GetComponent<SpawnObjects>())
		{
			GetComponent<SpawnObjects>().mostRecentlySpawnedObject.GetComponent<FollowPlayer>().enabled = false;
		}
	}
}
