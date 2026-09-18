using UnityEngine;
using UnityEngine.Events;

public class PlayerInRangeTrigger : MonoBehaviour
{
	public enum TargetType
	{
		Any = 0,
		OtherPlayer = 1
	}

	public TargetType targetType;

	public float range = 5f;

	public float cooldown;

	public bool repeating;

	private float counter;

	private bool done;

	[HideInInspector]
	public bool inRange;

	public UnityEvent triggerEvent;

	[HideInInspector]
	public Player target;

	private Player ownPlayer;

	public bool scaleWithRange;

	private void Start()
	{
		ownPlayer = base.transform.root.GetComponent<Player>();
		if (!ownPlayer)
		{
			ownPlayer = base.transform.root.GetComponentInParent<SpawnedAttack>().spawner;
		}
		if (scaleWithRange)
		{
			range *= base.transform.localScale.x;
		}
	}

	private void Update()
	{
		counter += TimeHandler.deltaTime;
		inRange = false;
		target = null;
		if (done)
		{
			return;
		}
		Player player = null;
		if (targetType == TargetType.OtherPlayer)
		{
			player = PlayerManager.instance.GetOtherPlayer(ownPlayer);
		}
		if (targetType == TargetType.Any)
		{
			player = PlayerManager.instance.GetClosestPlayer(base.transform.position);
		}
		if (PlayerManager.instance.CanSeePlayer(base.transform.position, player).canSee && Vector3.Distance(base.transform.position, player.transform.position) < range * base.transform.root.localScale.x && !player.data.dead && counter >= cooldown)
		{
			counter = 0f;
			triggerEvent.Invoke();
			inRange = true;
			target = player;
			if (!repeating)
			{
				done = true;
			}
		}
	}
}
