using UnityEngine;

public class LineRangeEffect : MonoBehaviour
{
	public float dmg;

	public float knockback;

	private LineEffect lineEffect;

	private Player owner;

	private SpawnedAttack spawned;

	private bool done;

	private void Start()
	{
		spawned = GetComponent<SpawnedAttack>();
		owner = GetComponent<SpawnedAttack>().spawner;
		lineEffect = GetComponentInChildren<LineEffect>();
	}

	private void Update()
	{
		if (done || !spawned.IsMine())
		{
			return;
		}
		Player closestPlayerInTeam = PlayerManager.instance.GetClosestPlayerInTeam(base.transform.position, PlayerManager.instance.GetOtherTeam(owner.teamID), needVision: true);
		if ((bool)closestPlayerInTeam)
		{
			float num = 2f;
			float radius = lineEffect.GetRadius();
			float num2 = Vector2.Distance(base.transform.position, closestPlayerInTeam.transform.position);
			if (num2 < radius + num && num2 > radius - num)
			{
				done = true;
				closestPlayerInTeam.data.healthHandler.CallTakeDamage(dmg * base.transform.localScale.x * (closestPlayerInTeam.transform.position - base.transform.position).normalized, closestPlayerInTeam.transform.position, null, owner);
				closestPlayerInTeam.data.healthHandler.CallTakeForce(knockback * base.transform.localScale.x * (closestPlayerInTeam.transform.position - base.transform.position).normalized);
			}
		}
	}
}
