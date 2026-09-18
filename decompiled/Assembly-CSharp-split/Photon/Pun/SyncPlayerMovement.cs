using System;
using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun;

[AddComponentMenu("Photon Networking/Photon Transform View")]
[RequireComponent(typeof(PhotonView))]
public class SyncPlayerMovement : MonoBehaviour, IPunObservable
{
	private PhotonView photonView;

	private CharacterData data;

	private float sincePushed;

	private bool didJump;

	private float lastTime;

	private List<PlayerSyncPackage> syncPackages = new List<PlayerSyncPackage>();

	public void Awake()
	{
		photonView = GetComponent<PhotonView>();
		data = GetComponent<CharacterData>();
		if (!photonView.IsMine)
		{
			data.input.controlledElseWhere = true;
			GetComponent<PlayerCollision>().checkForGoThroughWall = false;
			return;
		}
		PlayerJump jump = data.jump;
		jump.JumpAction = (Action)Delegate.Combine(jump.JumpAction, new Action(DoJump));
		data.weaponHandler.gun.AddAttackAction(SendShoot);
		Block block = data.block;
		block.BlockAction = (Action<BlockTrigger.BlockTriggerType>)Delegate.Combine(block.BlockAction, new Action<BlockTrigger.BlockTriggerType>(SendBlock));
	}

	private void OnEnable()
	{
		syncPackages.Clear();
	}

	public void SetDontSyncFor(float t)
	{
		sincePushed = t * 0.5f;
	}

	private void Update()
	{
		if (syncPackages.Count <= 0)
		{
			return;
		}
		if (syncPackages[0].timeDelta > 0f)
		{
			syncPackages[0].timeDelta -= Time.deltaTime * 1.5f * (1f + (float)syncPackages.Count * 0.1f);
			return;
		}
		while (syncPackages.Count > 5)
		{
			syncPackages.RemoveAt(0);
		}
		sincePushed -= Time.deltaTime;
		float t = Mathf.Clamp(1f - sincePushed * 5f, 0f, 1f);
		base.transform.position = Vector2.Lerp(base.transform.position, syncPackages[0].pos, t);
		data.playerVel.velocity = Vector2.Lerp(data.playerVel.velocity, syncPackages[0].vel, t);
		data.input.direction = syncPackages[0].dir;
		data.input.aimDirection = syncPackages[0].aim;
		data.input.jumpIsPressed = syncPackages[0].holdJump;
		data.sinceGrounded = syncPackages[0].sinceGrounded;
		if (syncPackages[0].jump)
		{
			Jump();
		}
		syncPackages.RemoveAt(0);
	}

	private void SendBlock(BlockTrigger.BlockTriggerType blockTrigger)
	{
		SendBlock(blockTrigger, toAll: false);
	}

	public void SendBlock(BlockTrigger.BlockTriggerType blockTrigger, bool toAll = false)
	{
		if (blockTrigger == BlockTrigger.BlockTriggerType.Default)
		{
			if (toAll)
			{
				data.view.RPC("RPCAO_DoBlock", RpcTarget.All);
			}
			else
			{
				data.view.RPC("RPCAO_DoBlock", RpcTarget.Others);
			}
		}
	}

	[PunRPC]
	public void RPCAO_DoBlock()
	{
		data.block.RPCA_DoBlock(firstBlock: true);
	}

	public void SendBlock(BlockTrigger.BlockTriggerType blockTrigger, bool firstBlock, bool dontSetCD)
	{
		data.view.RPC("RPCA_DoBlock", RpcTarget.All, (int)blockTrigger, firstBlock, dontSetCD);
	}

	[PunRPC]
	public void RPCA_DoBlock(int blocktTrigger, bool firstBlock, bool dontSetCD)
	{
		data.block.RPCA_DoBlock(firstBlock, dontSetCD, (BlockTrigger.BlockTriggerType)blocktTrigger);
	}

	public void SendShoot()
	{
		photonView.RPC("Shoot", RpcTarget.Others);
	}

	[PunRPC]
	public void Shoot()
	{
		data.weaponHandler.gun.Attack(0f, forceAttack: true);
	}

	public void DoJump()
	{
		didJump = true;
	}

	public void SendJump()
	{
		photonView.RPC("Jump", RpcTarget.Others);
	}

	[PunRPC]
	public void Jump()
	{
		data.jump.Jump(forceJump: true);
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(base.transform.position);
			stream.SendNext(data.input.direction);
			stream.SendNext(data.input.aimDirection);
			stream.SendNext(data.input.jumpIsPressed);
			stream.SendNext(data.playerVel.velocity);
			stream.SendNext(didJump);
			stream.SendNext(data.sinceGrounded);
			didJump = false;
			if (lastTime == 0f)
			{
				lastTime = Time.time;
			}
			stream.SendNext(Time.time - lastTime);
			lastTime = Time.time;
		}
		else
		{
			PlayerSyncPackage playerSyncPackage = new PlayerSyncPackage();
			playerSyncPackage.pos = (Vector3)stream.ReceiveNext();
			playerSyncPackage.dir = (Vector3)stream.ReceiveNext();
			playerSyncPackage.aim = (Vector3)stream.ReceiveNext();
			playerSyncPackage.holdJump = (bool)stream.ReceiveNext();
			playerSyncPackage.vel = (Vector2)stream.ReceiveNext();
			playerSyncPackage.jump = (bool)stream.ReceiveNext();
			playerSyncPackage.sinceGrounded = (float)stream.ReceiveNext();
			playerSyncPackage.timeDelta = (float)stream.ReceiveNext();
			playerSyncPackage.timeDelta = Mathf.Clamp(playerSyncPackage.timeDelta, 0f, 0.1f);
			syncPackages.Add(playerSyncPackage);
		}
	}
}
