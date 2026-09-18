using Photon.Pun;
using UnityEngine;

public class SpawnedAttack : MonoBehaviour
{
	public Player spawner;

	public int attackLevel;

	public int attackID;

	public PhotonView view;

	private void Awake()
	{
		view = GetComponent<PhotonView>();
	}

	[PunRPC]
	public void RPCA_SetSpawner(int spawnerID)
	{
		spawner = PhotonNetwork.GetPhotonView(spawnerID).GetComponent<Player>();
	}

	public void CopySpawnedAttackTo(GameObject to)
	{
		SpawnedAttack spawnedAttack = to.GetComponent<SpawnedAttack>();
		if (!spawnedAttack)
		{
			spawnedAttack = to.AddComponent<SpawnedAttack>();
		}
		spawnedAttack.spawner = spawner;
		spawnedAttack.attackLevel = attackLevel;
	}

	public void SetColor(Color color)
	{
		TrailRenderer[] componentsInChildren = GetComponentsInChildren<TrailRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].startColor = color;
			componentsInChildren[i].endColor = color;
		}
		ProjectileHit component = GetComponent<ProjectileHit>();
		if ((bool)component)
		{
			component.projectileColor = color;
		}
	}

	public bool IsMine()
	{
		if ((bool)view)
		{
			return view.IsMine;
		}
		if ((bool)spawner)
		{
			return spawner.data.view.IsMine;
		}
		return false;
	}
}
