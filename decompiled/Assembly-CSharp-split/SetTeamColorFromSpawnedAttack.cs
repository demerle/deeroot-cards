using UnityEngine;

public class SetTeamColorFromSpawnedAttack : MonoBehaviour
{
	private void Start()
	{
		GetComponent<SetTeamColor>().Set(PlayerSkinBank.GetPlayerSkinColors(GetComponentInParent<SpawnedAttack>().spawner.playerID));
	}
}
