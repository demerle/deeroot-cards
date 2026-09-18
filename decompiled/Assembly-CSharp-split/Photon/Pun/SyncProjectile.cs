using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun;

[AddComponentMenu("Photon Networking/Photon Transform View")]
[RequireComponent(typeof(PhotonView))]
public class SyncProjectile : MonoBehaviour, IPunObservable
{
	public bool active;

	private PhotonView photonView;

	private MoveTransform move;

	private float lastTime;

	private List<ProjectileSyncPackage> syncPackages = new List<ProjectileSyncPackage>();

	public void Awake()
	{
		photonView = GetComponent<PhotonView>();
		move = GetComponent<MoveTransform>();
		_ = photonView.IsMine;
	}

	private void Update()
	{
		if (!active || syncPackages.Count <= 0)
		{
			return;
		}
		if (syncPackages[0].timeDelta > 0f)
		{
			syncPackages[0].timeDelta -= Time.deltaTime * 1.5f * (1f + (float)syncPackages.Count * 0.5f);
			return;
		}
		if (syncPackages.Count > 2)
		{
			syncPackages.RemoveAt(0);
		}
		base.transform.position = syncPackages[0].pos;
		move.velocity = syncPackages[0].vel;
		syncPackages.RemoveAt(0);
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (!active)
		{
			return;
		}
		if (stream.IsWriting)
		{
			stream.SendNext(base.transform.position);
			stream.SendNext(move.velocity);
			if (lastTime == 0f)
			{
				lastTime = Time.time;
			}
			stream.SendNext(Time.time - lastTime);
			lastTime = Time.time;
		}
		else
		{
			ProjectileSyncPackage projectileSyncPackage = new ProjectileSyncPackage();
			projectileSyncPackage.pos = (Vector3)stream.ReceiveNext();
			projectileSyncPackage.vel = (Vector3)stream.ReceiveNext();
			projectileSyncPackage.timeDelta = (float)stream.ReceiveNext();
			syncPackages.Add(projectileSyncPackage);
		}
	}
}
