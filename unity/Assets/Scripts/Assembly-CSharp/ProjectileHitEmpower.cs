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
		GetComponentInParent<SpawnedAttack>().spawner.data.block.DoBlockAtPosition(true, true, BlockTrigger.BlockTriggerType.Empower, hit.point - (Vector2)base.transform.forward * 0.05f, true);
		done = true;
		return HasToReturn.canContinue;
	}
}
