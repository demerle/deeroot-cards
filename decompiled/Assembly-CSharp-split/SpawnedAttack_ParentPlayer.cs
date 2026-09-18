using UnityEngine;

public class SpawnedAttack_ParentPlayer : MonoBehaviour
{
	private void Start()
	{
		GetComponent<SpawnedAttack>().spawner = GetComponentInParent<Player>();
	}
}
