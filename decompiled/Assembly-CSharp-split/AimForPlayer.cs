using UnityEngine;

public class AimForPlayer : MonoBehaviour
{
	public enum Target
	{
		OtherPlayer,
		Closest
	}

	public float upOffset;

	public Target target;

	private SpawnedAttack spawned;

	private void Start()
	{
		spawned = GetComponentInParent<SpawnedAttack>();
	}

	private void Update()
	{
		Player player = null;
		player = ((target != Target.OtherPlayer) ? PlayerManager.instance.GetClosestPlayer(base.transform.position, needVision: true) : PlayerManager.instance.GetOtherPlayer(spawned.spawner));
		if ((bool)player && PlayerManager.instance.CanSeePlayer(base.transform.position, player).canSee)
		{
			base.transform.rotation = Quaternion.LookRotation(player.transform.position + Vector3.up * Vector3.Distance(player.transform.position, base.transform.position) * 0.1f * upOffset - base.transform.position, Vector3.forward);
		}
	}
}
