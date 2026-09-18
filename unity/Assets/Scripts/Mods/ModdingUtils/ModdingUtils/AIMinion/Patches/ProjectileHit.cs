using System;
using HarmonyLib;
using UnityEngine;
using UnboundLib;
using Photon.Pun;

namespace ModdingUtils.AIMinion.Patches
{
	[Serializable]
	[HarmonyPatch(typeof(ProjectileHit), "RPCA_DoHit")]
	[HarmonyPriority(Priority.First)]
	class ProjectileHitPatchRPCA_DoHit
	{
		// prefix to allow autoblocking
		private static void Prefix(ProjectileHit __instance, Vector2 hitPoint, Vector2 hitNormal, Vector2 vel, int viewID, int colliderID, ref bool wasBlocked)
		{
			HitInfo hitInfo = new HitInfo();
			hitInfo.point = hitPoint;
			hitInfo.normal = hitNormal;
			hitInfo.collider = null;
			if (viewID != -1)
			{
				PhotonView photonView = PhotonNetwork.GetPhotonView(viewID);
				hitInfo.collider = photonView.GetComponentInChildren<Collider2D>();
				hitInfo.transform = photonView.transform;
			}
			else if (colliderID != -1)
			{
				hitInfo.collider = MapManager.instance.currentMap.Map.GetComponentsInChildren<Collider2D>()[colliderID];
				hitInfo.transform = hitInfo.collider.transform;
			}
			HealthHandler healthHandler = null;
			if (hitInfo.transform)
			{
				healthHandler = hitInfo.transform.GetComponent<HealthHandler>();
			}
			if (healthHandler && healthHandler.GetComponent<CharacterData>() && healthHandler.GetComponent<Block>())
			{
				Block block = healthHandler.GetComponent<Block>();
				if (Extensions.CharacterDataExtension.GetAdditionalData(healthHandler.GetComponent<CharacterData>()).autoBlock && block.counter >= block.Cooldown())
                {
					wasBlocked = true;
					if (healthHandler.GetComponent<CharacterData>().view.IsMine) { block.TryBlock(); }
				}
			}
		}
	}
}
