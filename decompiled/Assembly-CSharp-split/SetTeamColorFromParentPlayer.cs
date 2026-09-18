using UnityEngine;

public class SetTeamColorFromParentPlayer : MonoBehaviour
{
	private void Start()
	{
		Player player = GetComponentInParent<Player>();
		if (!player)
		{
			player = GetComponentInParent<SpawnedAttack>().spawner;
		}
		GetComponent<SetTeamColor>().Set(PlayerSkinBank.GetPlayerSkinColors(player.playerID));
	}
}
