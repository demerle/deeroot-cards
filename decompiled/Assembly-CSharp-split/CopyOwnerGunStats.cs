using UnityEngine;

public class CopyOwnerGunStats : MonoBehaviour
{
	private void Start()
	{
		ApplyCardStats.CopyGunStats(base.transform.root.GetComponent<SpawnedAttack>().spawner.GetComponent<WeaponHandler>().gun, GetComponent<Gun>());
	}
}
