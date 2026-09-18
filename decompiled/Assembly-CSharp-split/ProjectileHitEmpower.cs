using UnityEngine;

public class ProjectileHitEmpower : RayHitEffect
{
	private bool done;

	public override HasToReturn DoHitEffect(HitInfo hit)
	{
		if (done)
		{
			return HasToReturn.canContinue;
		}
		GetComponentInParent<SpawnedAttack>().spawner.data.block.DoBlockAtPosition(firstBlock: true, dontSetCD: true, BlockTrigger.BlockTriggerType.Empower, hit.point - (Vector2)base.transform.forward * 0.05f, onlyBlockEffects: true);
		done = true;
		return HasToReturn.canContinue;
	}
}
