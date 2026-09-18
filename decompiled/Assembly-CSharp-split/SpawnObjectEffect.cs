using Sirenix.OdinInspector;
using UnityEngine;

public class SpawnObjectEffect : MonoBehaviour
{
	public enum Dir
	{
		TowardsEnemy,
		DamageDirction,
		ReverseDamageDir,
		TowardsRecentlyDamaged
	}

	public Dir dir;

	[FoldoutGroup("Gun", 0)]
	public bool spawnAttack;

	[FoldoutGroup("Gun", 0)]
	public float bulletDamageMultiplier = 0.5f;

	[FoldoutGroup("Gun", 0)]
	public int numberOfattacks = 1;

	[FoldoutGroup("Gun", 0)]
	public float spread;

	[FoldoutGroup("Gun", 0)]
	public float gravityMultiplier = 1f;

	[FoldoutGroup("Gun", 0)]
	public float speedMultiplier = 1f;

	[FoldoutGroup("Gun", 0)]
	public float extraSpread;

	[FoldoutGroup("Gun", 0)]
	public float recoilMultiplier = 1f;

	[FoldoutGroup("Gun", 0)]
	public int maxBullets = 100;

	private Holding holding;

	private Gun gun;

	private Player player;

	private void Start()
	{
		holding = GetComponentInParent<Holding>();
		player = GetComponentInParent<Player>();
	}

	public void DoEffect(Vector2 dmg)
	{
		int num = 0;
		if (!spawnAttack)
		{
			return;
		}
		for (int i = 0; i < numberOfattacks; i++)
		{
			if (!gun && (bool)holding && (bool)holding.holdable)
			{
				gun = holding.holdable.GetComponent<Gun>();
			}
			if ((bool)gun)
			{
				Quaternion rotation = gun.transform.rotation;
				Vector3 vector = (PlayerManager.instance.GetOtherPlayer(player).transform.position - base.transform.position).normalized;
				if (dir == Dir.DamageDirction)
				{
					vector = dmg.normalized;
				}
				if (dir == Dir.ReverseDamageDir)
				{
					vector = -dmg.normalized;
				}
				if (dir == Dir.TowardsRecentlyDamaged)
				{
					vector = (player.data.lastDamagedPlayer.transform.position - base.transform.position).normalized;
				}
				gun.transform.rotation = Quaternion.LookRotation(Vector3.forward, vector + gun.transform.right * Random.Range(0f - spread, spread));
				gun.projectileSpeed *= speedMultiplier;
				gun.gravity *= gravityMultiplier;
				gun.spread += extraSpread;
				gun.Attack(1f, forceAttack: true, bulletDamageMultiplier, recoilMultiplier);
				num += (int)((float)gun.numberOfProjectiles + gun.chargeNumberOfProjectilesTo);
				gun.projectileSpeed /= speedMultiplier;
				gun.gravity /= gravityMultiplier;
				gun.spread -= extraSpread;
				gun.transform.rotation = rotation;
				if (num > maxBullets)
				{
					break;
				}
			}
		}
	}
}
