using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
	public enum Target
	{
		Self,
		Other
	}

	public Target target;

	public bool inheritScale = true;

	private Vector3 startScale;

	private Player ownPlayer;

	private void Start()
	{
		startScale = base.transform.localScale;
		ownPlayer = GetComponentInParent<Player>();
		if (!ownPlayer)
		{
			ownPlayer = GetComponentInParent<SpawnedAttack>().spawner;
		}
	}

	private void LateUpdate()
	{
		Player player = null;
		player = ((target != Target.Other) ? ownPlayer : PlayerManager.instance.GetOtherPlayer(ownPlayer));
		if (inheritScale)
		{
			base.transform.localScale = new Vector3(player.transform.localScale.x * startScale.x, player.transform.localScale.y * startScale.y, player.transform.localScale.z * startScale.z);
		}
		base.transform.position = player.transform.position;
		base.transform.rotation = player.transform.rotation;
	}
}
