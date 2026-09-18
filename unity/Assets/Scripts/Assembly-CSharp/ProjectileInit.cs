using Photon.Pun;
using UnityEngine;

public class ProjectileInit : MonoBehaviour
{
	private Gun[] guns;

	[PunRPC]
	internal void RPCA_Init(int senderID, int nrOfProj, float dmgM, float randomSeed)
	{
		PlayerManager.instance.GetPlayerWithActorID(senderID).data.weaponHandler.gun.BulletInit(base.gameObject, nrOfProj, dmgM, randomSeed);
	}

	internal void OFFLINE_Init(int senderID, int nrOfProj, float dmgM, float randomSeed)
	{
		PlayerManager.instance.players[senderID].data.weaponHandler.gun.BulletInit(base.gameObject, nrOfProj, dmgM, randomSeed);
	}

	[PunRPC]
	internal void RPCA_Init_SeparateGun(int senderID, int gunID, int nrOfProj, float dmgM, float randomSeed)
	{
		GetChildGunWithID(gunID, PlayerManager.instance.GetPlayerWithActorID(senderID).gameObject).BulletInit(base.gameObject, nrOfProj, dmgM, randomSeed);
	}

	internal void OFFLINE_Init_SeparateGun(int senderID, int gunID, int nrOfProj, float dmgM, float randomSeed)
	{
		GetChildGunWithID(gunID, PlayerManager.instance.players[senderID].gameObject).BulletInit(base.gameObject, nrOfProj, dmgM, randomSeed);
	}

	private Gun GetChildGunWithID(int id, GameObject player)
	{
		if (guns == null)
		{
			guns = player.GetComponentsInChildren<Gun>();
		}
		return guns[id];
	}

	[PunRPC]
	internal void RPCA_Init_noAmmoUse(int senderID, int nrOfProj, float dmgM, float randomSeed)
	{
		PlayerManager.instance.GetPlayerWithActorID(senderID).data.weaponHandler.gun.BulletInit(base.gameObject, nrOfProj, dmgM, randomSeed, false);
	}

	internal void OFFLINE_Init_noAmmoUse(int senderID, int nrOfProj, float dmgM, float randomSeed)
	{
		PlayerManager.instance.players[senderID].data.weaponHandler.gun.BulletInit(base.gameObject, nrOfProj, dmgM, randomSeed, false);
	}
}
