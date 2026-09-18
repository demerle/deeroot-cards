using UnityEngine;

public class FollowRandomPlayer : MonoBehaviour
{
	public bool X = true;

	public bool Y = true;

	private Transform targetPlayer;

	public bool snap;

	private void Start()
	{
	}

	private void Update()
	{
		if (!targetPlayer)
		{
			GetNewPlayer();
		}
		else if (snap)
		{
			base.transform.position = new Vector3(X ? targetPlayer.position.x : base.transform.position.x, Y ? targetPlayer.position.y : base.transform.position.y, 0f);
		}
		else
		{
			base.transform.position = Vector3.Lerp(base.transform.position, new Vector3(X ? targetPlayer.position.x : base.transform.position.x, Y ? targetPlayer.position.y : base.transform.position.y, 0f), TimeHandler.deltaTime * 5f);
		}
	}

	private void GetNewPlayer()
	{
		if (PlayerManager.instance.players.Count > 0)
		{
			targetPlayer = PlayerManager.instance.players[Random.Range(0, PlayerManager.instance.players.Count)].transform;
		}
	}
}
