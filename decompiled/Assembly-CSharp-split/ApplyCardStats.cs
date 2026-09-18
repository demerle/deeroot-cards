using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class ApplyCardStats : MonoBehaviour
{
	private Gun myGunStats;

	private CharacterStatModifiers myPlayerStats;

	private Block myBlock;

	private Player playerToUpgrade;

	private CardAudioModifier cardAudio;

	private bool done;

	private DamagableEvent damagable;

	public bool shootToPick;

	private void Start()
	{
		myGunStats = GetComponent<Gun>();
		myPlayerStats = GetComponent<CharacterStatModifiers>();
		myBlock = GetComponentInChildren<Block>();
		cardAudio = GetComponent<CardAudioModifier>();
		damagable = GetComponentInChildren<DamagableEvent>();
	}

	private void Update()
	{
		if (shootToPick && damagable.dead && (bool)damagable.lastPlayer)
		{
			Pick(damagable.lastPlayer.teamID);
			Object.Destroy(base.gameObject);
		}
	}

	[PunRPC]
	public void RPCA_Pick(int[] actorIDs)
	{
		for (int i = 0; i < actorIDs.Length; i++)
		{
			playerToUpgrade = PlayerManager.instance.GetPlayerWithActorID(actorIDs[i]);
			ApplyStats();
			CardBarHandler.instance.AddCard(playerToUpgrade.playerID, GetComponent<CardInfo>().sourceCard);
		}
	}

	[PunRPC]
	public void OFFLINE_Pick(Player[] players)
	{
		for (int i = 0; i < players.Length; i++)
		{
			playerToUpgrade = players[i];
			ApplyStats();
			CardBarHandler.instance.AddCard(playerToUpgrade.playerID, GetComponent<CardInfo>().sourceCard);
		}
	}

	public void Pick(int pickerID, bool forcePick = false, PickerType pickerType = PickerType.Team)
	{
		Start();
		if (done && !forcePick)
		{
			return;
		}
		done = true;
		Player[] array = PlayerManager.instance.GetPlayersInTeam(pickerID);
		if (pickerType == PickerType.Player)
		{
			array = new Player[1] { PlayerManager.instance.players[pickerID] };
		}
		if (PhotonNetwork.OfflineMode)
		{
			OFFLINE_Pick(array);
			return;
		}
		int[] array2 = new int[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = array[i].data.view.ControllerActorNr;
		}
		GetComponent<PhotonView>().RPC("RPCA_Pick", RpcTarget.All, array2);
	}

	private void ApplyStats()
	{
		done = true;
		PlayerAudioModifyers component = playerToUpgrade.GetComponent<PlayerAudioModifyers>();
		Gun component2 = playerToUpgrade.GetComponent<Holding>().holdable.GetComponent<Gun>();
		Player component3 = playerToUpgrade.GetComponent<Player>();
		CharacterData component4 = playerToUpgrade.GetComponent<CharacterData>();
		HealthHandler component5 = playerToUpgrade.GetComponent<HealthHandler>();
		playerToUpgrade.GetComponent<Movement>();
		Gravity component6 = playerToUpgrade.GetComponent<Gravity>();
		Block component7 = playerToUpgrade.GetComponent<Block>();
		CharacterStatModifiers component8 = component3.GetComponent<CharacterStatModifiers>();
		GunAmmo componentInChildren = component2.GetComponentInChildren<GunAmmo>();
		if ((bool)componentInChildren && (bool)myGunStats)
		{
			componentInChildren.ammoReg += myGunStats.ammoReg;
			componentInChildren.maxAmmo += myGunStats.ammo;
			componentInChildren.maxAmmo = Mathf.Clamp(componentInChildren.maxAmmo, 1, 90);
			componentInChildren.reloadTimeMultiplier *= myGunStats.reloadTime;
			componentInChildren.reloadTimeAdd += myGunStats.reloadTimeAdd;
		}
		component3.data.currentCards.Add(GetComponent<CardInfo>().sourceCard);
		if ((bool)myGunStats)
		{
			if (myGunStats.lockGunToDefault)
			{
				component2.defaultCooldown = myGunStats.forceSpecificAttackSpeed;
				component2.lockGunToDefault = myGunStats.lockGunToDefault;
			}
			if ((bool)myGunStats && myGunStats.projectiles.Length != 0)
			{
				component2.projectiles[0].objectToSpawn = myGunStats.projectiles[0].objectToSpawn;
			}
			if ((bool)myGunStats)
			{
				CopyGunStats(myGunStats, component2);
			}
		}
		if ((bool)myPlayerStats)
		{
			component8.sizeMultiplier *= myPlayerStats.sizeMultiplier;
			component4.maxHealth *= myPlayerStats.health;
			component8.ConfigureMassAndSize();
			component8.movementSpeed *= myPlayerStats.movementSpeed;
			component8.jump *= myPlayerStats.jump;
			component4.jumps += myPlayerStats.numberOfJumps;
			component6.gravityForce *= myPlayerStats.gravity;
			component5.regeneration += myPlayerStats.regen;
			if ((bool)myPlayerStats.AddObjectToPlayer)
			{
				component8.objectsAddedToPlayer.Add(Object.Instantiate(myPlayerStats.AddObjectToPlayer, component3.transform.position, component3.transform.rotation, component3.transform));
			}
			component8.lifeSteal += myPlayerStats.lifeSteal;
			component8.respawns += myPlayerStats.respawns;
			component8.secondsToTakeDamageOver += myPlayerStats.secondsToTakeDamageOver;
			if (myPlayerStats.refreshOnDamage)
			{
				component8.refreshOnDamage = true;
			}
			if (!myPlayerStats.automaticReload)
			{
				component8.automaticReload = false;
			}
		}
		if ((bool)myBlock)
		{
			if (myBlock.objectsToSpawn != null)
			{
				for (int i = 0; i < myBlock.objectsToSpawn.Count; i++)
				{
					component7.objectsToSpawn.Add(myBlock.objectsToSpawn[i]);
				}
			}
			component7.cdMultiplier *= myBlock.cdMultiplier;
			component7.cdAdd += myBlock.cdAdd;
			component7.forceToAdd += myBlock.forceToAdd;
			component7.forceToAddUp += myBlock.forceToAddUp;
			component7.additionalBlocks += myBlock.additionalBlocks;
			component7.healing += myBlock.healing;
			if (myBlock.autoBlock)
			{
				component7.autoBlock = myBlock.autoBlock;
			}
		}
		if ((bool)component && (bool)cardAudio)
		{
			component.AddToStack(cardAudio);
		}
		component8.WasUpdated();
		component5.Revive();
	}

	public static void CopyGunStats(Gun copyFromGun, Gun copyToGun)
	{
		if (copyFromGun.unblockable)
		{
			copyToGun.unblockable = copyFromGun.unblockable;
		}
		if (copyFromGun.ignoreWalls)
		{
			copyToGun.ignoreWalls = copyFromGun.ignoreWalls;
		}
		float num = 1f;
		if (copyFromGun.numberOfProjectiles != 0 && copyToGun.numberOfProjectiles != 1)
		{
			num = (float)copyFromGun.numberOfProjectiles / ((float)copyFromGun.numberOfProjectiles + (float)copyToGun.numberOfProjectiles);
		}
		copyToGun.damage *= 1f - num * (1f - copyFromGun.damage);
		if (copyToGun.damage < 0.25f)
		{
			copyToGun.damage = 0.25f;
		}
		copyToGun.size += copyFromGun.size;
		float num2 = 1f;
		if (copyFromGun.chargeNumberOfProjectilesTo != 0f)
		{
			num2 = copyFromGun.chargeNumberOfProjectilesTo / (copyFromGun.chargeNumberOfProjectilesTo + copyToGun.chargeNumberOfProjectilesTo);
		}
		copyToGun.chargeDamageMultiplier *= 1f - num2 * (1f - copyFromGun.chargeDamageMultiplier);
		copyToGun.knockback *= 1f - num * (1f - copyFromGun.knockback);
		copyToGun.projectileSpeed *= copyFromGun.projectileSpeed;
		copyToGun.projectielSimulatonSpeed *= copyFromGun.projectielSimulatonSpeed;
		copyToGun.gravity *= copyFromGun.gravity;
		copyToGun.multiplySpread *= copyFromGun.multiplySpread;
		copyToGun.attackSpeed *= copyFromGun.attackSpeed;
		copyToGun.bodyRecoil *= copyFromGun.recoilMuiltiplier;
		copyToGun.speedMOnBounce *= copyFromGun.speedMOnBounce;
		copyToGun.dmgMOnBounce *= copyFromGun.dmgMOnBounce;
		copyToGun.bulletDamageMultiplier *= copyFromGun.bulletDamageMultiplier;
		copyToGun.spread += copyFromGun.spread;
		copyToGun.drag += copyFromGun.drag;
		copyToGun.timeBetweenBullets += copyFromGun.timeBetweenBullets;
		copyToGun.dragMinSpeed += copyFromGun.dragMinSpeed;
		copyToGun.evenSpread += copyFromGun.evenSpread;
		copyToGun.numberOfProjectiles += copyFromGun.numberOfProjectiles;
		copyToGun.reflects += copyFromGun.reflects;
		copyToGun.smartBounce += copyFromGun.smartBounce;
		copyToGun.bulletPortal += copyFromGun.bulletPortal;
		copyToGun.randomBounces += copyFromGun.randomBounces;
		copyToGun.bursts += copyFromGun.bursts;
		copyToGun.slow += copyFromGun.slow;
		copyToGun.overheatMultiplier += copyFromGun.overheatMultiplier;
		copyToGun.projectileSize += copyFromGun.projectileSize;
		copyToGun.percentageDamage += copyFromGun.percentageDamage;
		copyToGun.damageAfterDistanceMultiplier *= copyFromGun.damageAfterDistanceMultiplier;
		copyToGun.timeToReachFullMovementMultiplier *= copyFromGun.timeToReachFullMovementMultiplier;
		copyToGun.cos += copyFromGun.cos;
		if (copyFromGun.dontAllowAutoFire)
		{
			copyToGun.dontAllowAutoFire = true;
		}
		if (copyFromGun.destroyBulletAfter != 0f)
		{
			copyToGun.destroyBulletAfter = copyFromGun.destroyBulletAfter;
		}
		copyToGun.chargeSpreadTo += copyFromGun.chargeSpreadTo;
		copyToGun.chargeSpeedTo += copyFromGun.chargeSpeedTo;
		copyToGun.chargeEvenSpreadTo += copyFromGun.chargeEvenSpreadTo;
		copyToGun.chargeNumberOfProjectilesTo += copyFromGun.chargeNumberOfProjectilesTo;
		copyToGun.chargeRecoilTo += copyFromGun.chargeRecoilTo;
		if (copyFromGun.projectileColor != Color.black)
		{
			if (copyToGun.projectileColor == Color.black)
			{
				copyToGun.projectileColor = copyFromGun.projectileColor;
			}
			float r = Mathf.Pow((copyToGun.projectileColor.r * copyToGun.projectileColor.r + copyFromGun.projectileColor.r * copyFromGun.projectileColor.r) / 2f, 0.5f);
			float g = Mathf.Pow((copyToGun.projectileColor.g * copyToGun.projectileColor.g + copyFromGun.projectileColor.g * copyFromGun.projectileColor.g) / 2f, 0.5f);
			float b = Mathf.Pow((copyToGun.projectileColor.b * copyToGun.projectileColor.b + copyFromGun.projectileColor.b * copyFromGun.projectileColor.b) / 2f, 0.5f);
			Color rgbColor = new Color(r, g, b, 1f);
			float H = 0f;
			float S = 0f;
			float V = 0f;
			Color.RGBToHSV(rgbColor, out H, out S, out V);
			S = 1f;
			V = 1f;
			copyToGun.projectileColor = Color.HSVToRGB(H, S, V);
		}
		List<ObjectsToSpawn> list = new List<ObjectsToSpawn>();
		for (int i = 0; i < copyToGun.objectsToSpawn.Length; i++)
		{
			list.Add(copyToGun.objectsToSpawn[i]);
		}
		for (int j = 0; j < copyFromGun.objectsToSpawn.Length; j++)
		{
			bool flag = false;
			for (int k = 0; k < list.Count; k++)
			{
				if ((bool)list[k].effect && (bool)copyFromGun.objectsToSpawn[j].effect)
				{
					if (list[k].effect.name == copyFromGun.objectsToSpawn[j].effect.name && list[k].scaleStacks)
					{
						list[k].stacks++;
						flag = true;
					}
				}
				else if ((bool)list[k].AddToProjectile && (bool)copyFromGun.objectsToSpawn[j].AddToProjectile && list[k].AddToProjectile.name == copyFromGun.objectsToSpawn[j].AddToProjectile.name && list[k].scaleStacks)
				{
					list[k].stacks++;
					flag = true;
				}
			}
			if (!flag)
			{
				list.Add(copyFromGun.objectsToSpawn[j]);
			}
		}
		copyToGun.objectsToSpawn = list.ToArray();
		if (copyFromGun.useCharge)
		{
			copyToGun.useCharge = copyFromGun.useCharge;
		}
		copyToGun.soundGun.AddSoundShotModifier(copyFromGun.soundShotModifier);
		copyToGun.soundGun.AddSoundImpactModifier(copyFromGun.soundImpactModifier);
		copyToGun.soundGun.RefreshSoundModifiers();
	}
}
