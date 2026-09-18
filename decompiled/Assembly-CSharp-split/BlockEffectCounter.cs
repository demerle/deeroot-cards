using UnityEngine;

public class BlockEffectCounter : BlockEffect
{
	public int ownAttacksToSendBack;

	private SpawnObjectEffect spawn;

	private void Start()
	{
		spawn = GetComponentInParent<SpawnObjectEffect>();
	}

	public override void DoBlockedProjectile(GameObject projectile, Vector3 forward, Vector3 hitPos)
	{
		for (int i = 0; i < ownAttacksToSendBack; i++)
		{
			spawn.DoEffect(forward);
		}
	}
}
