using System;
using System.Collections;
using UnityEngine;

public class Teleport : MonoBehaviour
{
	public ParticleSystem[] parts;

	public ParticleSystem[] remainParts;

	public float distance = 10f;

	public LayerMask mask;

	private CharacterData data;

	private AttackLevel level;

	private void Start()
	{
		parts = GetComponentsInChildren<ParticleSystem>();
		data = GetComponentInParent<CharacterData>();
		level = GetComponentInParent<AttackLevel>();
		Block componentInParent = GetComponentInParent<Block>();
		componentInParent.SuperFirstBlockAction = (Action<BlockTrigger.BlockTriggerType>)Delegate.Combine(componentInParent.SuperFirstBlockAction, new Action<BlockTrigger.BlockTriggerType>(Go));
	}

	private void OnDestroy()
	{
		Block componentInParent = GetComponentInParent<Block>();
		componentInParent.SuperFirstBlockAction = (Action<BlockTrigger.BlockTriggerType>)Delegate.Remove(componentInParent.SuperFirstBlockAction, new Action<BlockTrigger.BlockTriggerType>(Go));
	}

	public void Go(BlockTrigger.BlockTriggerType triggerType)
	{
		StartCoroutine(DelayMove(triggerType, base.transform.position));
	}

	private IEnumerator DelayMove(BlockTrigger.BlockTriggerType triggerType, Vector3 beforePos)
	{
		if (triggerType == BlockTrigger.BlockTriggerType.Empower)
		{
			yield return new WaitForSeconds(0f);
		}
		Vector3 position = base.transform.position;
		Vector3 position2 = base.transform.position;
		int num = 10;
		float num2 = distance * (float)level.attackLevel / (float)num;
		for (int i = 0; i < num; i++)
		{
			position += num2 * data.aimDirection;
			if (!Physics2D.OverlapCircle(position, 0.5f))
			{
				position2 = position;
			}
		}
		for (int j = 0; j < remainParts.Length; j++)
		{
			remainParts[j].transform.position = base.transform.root.position;
			remainParts[j].Play();
		}
		GetComponentInParent<PlayerCollision>().IgnoreWallForFrames(2);
		if (triggerType == BlockTrigger.BlockTriggerType.Empower)
		{
			position2 = beforePos;
		}
		base.transform.root.position = position2;
		for (int k = 0; k < parts.Length; k++)
		{
			parts[k].transform.position = position2;
			parts[k].Play();
		}
		data.playerVel.velocity *= 0f;
		data.sinceGrounded = 0f;
	}
}
