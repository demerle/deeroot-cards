using UnityEngine;

public class ActivateSciptWhenCanSeeOtherPlayer : MonoBehaviour
{
	public enum Target
	{
		OtherPlayer = 0,
		Closest = 1
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
		player = ((target != 0) ? PlayerManager.instance.GetClosestPlayer(base.transform.position, true) : PlayerManager.instance.GetOtherPlayer(spawned.spawner));
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
