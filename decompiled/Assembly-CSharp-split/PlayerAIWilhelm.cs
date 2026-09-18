using UnityEngine;

public class PlayerAIWilhelm : MonoBehaviour
{
	private PlayerAPI api;

	private void Start()
	{
		api = GetComponentInParent<PlayerAPI>();
	}

	private void Update()
	{
		api.Move(api.TowardsOtherPlayer());
		api.SetAimDirection(api.TowardsOtherPlayer() + api.GetOtherPlayer().data.playerVel.velocity * 0.1f);
		api.Attack();
		api.Jump();
		api.Block();
	}
}
