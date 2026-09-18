using UnityEngine;

public class StunPlayer : MonoBehaviour
{
	public enum TargetPlayer
	{
		OtherPlayer = 0,
		Self = 1
	}

	public TargetPlayer targetPlayer;

	public float time = 0.5f;

	private Player target;

	public void Go()
	{
		if (!target)
		{
			target = GetComponentInParent<Player>();
			if (targetPlayer == TargetPlayer.OtherPlayer)
			{
				target = PlayerManager.instance.GetOtherPlayer(target);
			}
		}
		target.data.stunHandler.AddStun(time);
	}
}
