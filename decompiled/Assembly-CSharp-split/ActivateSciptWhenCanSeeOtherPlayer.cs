using UnityEngine;

public class ActivateSciptWhenCanSeeOtherPlayer : MonoBehaviour
{
	public enum Target
	{
		OtherPlayer,
		Closest
	}

	public Target target;

	private SpawnedAttack spawned;

	public MonoBehaviour script;

	private void Start()
	{
		spawned = GetComponentInParent<SpawnedAttack>();
	}

	private void Update()
	{
		Player player = null;
		player = ((target != Target.OtherPlayer) ? PlayerManager.instance.GetClosestPlayer(base.transform.position, needVision: true) : PlayerManager.instance.GetOtherPlayer(spawned.spawner));
		if ((bool)player)
		{
			if (PlayerManager.instance.CanSeePlayer(base.transform.position, player).canSee)
			{
				script.enabled = true;
			}
			else
			{
				script.enabled = false;
			}
		}
		else
		{
			script.enabled = false;
		}
	}
}
