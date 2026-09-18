using System.Collections.Generic;
using Photon.Pun;
using Sonigon;
using UnityEngine;

public class NetworkPhysicsObject : MonoBehaviour, IPunObservable
{
	[Header("Sounds")]
	public SoundEvent soundBoxImpact;

	public float soundPitchSemitone;

	private SoundParameterPitchSemitone soundParameterPitchSemitone = new SoundParameterPitchSemitone();

	private SoundParameterIntensity soundParameterIntensity = new SoundParameterIntensity();

	[Header("Settings")]
	public PhotonView photonView;

	private Rigidbody2D rig2D;

	private Collider2D col;

	public int sendFreq = 5;

	private int currentFrame;

	private float lastTime;

	private List<ObjectSyncPackage> syncPackages = new List<ObjectSyncPackage>();

	private float sinceCol;

	public float collisionThreshold;

	public float shakeAmount;

	public float maxShake;

	public float playerColThreshold = 1f;

	public float dmgAmount = 1f;

	public float forceAmount = 1f;

	private float sinceDealDMG;

	private List<Player> hitPlayers = new List<Player>();

	public float speed = 100f;

	private float sinceRequest;

	private float sincePushed;

	public float bulletPushMultiplier = 1f;

	private Vector2 currentForceToSend;

	private Vector2 currentForcePos;

	private float sendForceRate = 0.1f;

	private float sendForceCounter;

	public float sleepThreshold = 1f;

	public void Awake()
	{
		soundParameterPitchSemitone.pitchSemitone = soundPitchSemitone;
		currentFrame = Random.Range(0, sendFreq);
		photonView = GetComponent<PhotonView>();
		col = GetComponent<Collider2D>();
		rig2D = GetComponent<Rigidbody2D>();
	}

	private void Update()
	{
		if (!photonView)
		{
			return;
		}
		sinceRequest += Time.deltaTime;
		sinceDealDMG += Time.deltaTime;
		sendForceCounter += Time.deltaTime;
		if (sendForceCounter > sendForceRate)
		{
			_ = photonView.IsMine;
			if (currentForceToSend != Vector2.zero)
			{
				photonView.RPC("RPCA_SendForce", photonView.Owner, currentForceToSend, currentForcePos);
				sendForceCounter = 0f;
				currentForceToSend = Vector2.zero;
			}
		}
		if (syncPackages.Count > 0)
		{
			if (syncPackages[0].timeDelta > 0f)
			{
				syncPackages[0].timeDelta -= Time.deltaTime * 1.5f * (1f + (float)syncPackages.Count * 0.5f);
			}
			else
			{
				if (syncPackages.Count > 2)
				{
					syncPackages.RemoveAt(0);
				}
				rig2D.isKinematic = false;
				base.transform.position = syncPackages[0].pos;
				base.transform.rotation = Quaternion.LookRotation(Vector3.forward, syncPackages[0].rot);
				rig2D.velocity = syncPackages[0].vel;
				rig2D.angularVelocity = syncPackages[0].angularVel;
				syncPackages.RemoveAt(0);
			}
		}
		sinceCol += Time.deltaTime;
	}

	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		currentFrame++;
		if (stream.IsWriting)
		{
			if (currentFrame >= sendFreq)
			{
				currentFrame = 0;
				stream.SendNext((Vector2)base.transform.position);
				stream.SendNext((Vector2)base.transform.up);
				stream.SendNext(rig2D.velocity);
				stream.SendNext(rig2D.angularVelocity);
				if (lastTime == 0f)
				{
					lastTime = Time.time;
				}
				stream.SendNext(Time.time - lastTime);
				lastTime = Time.time;
			}
		}
		else
		{
			ObjectSyncPackage objectSyncPackage = new ObjectSyncPackage();
			objectSyncPackage.pos = (Vector2)stream.ReceiveNext();
			objectSyncPackage.rot = (Vector2)stream.ReceiveNext();
			objectSyncPackage.vel = (Vector2)stream.ReceiveNext();
			objectSyncPackage.angularVel = (float)stream.ReceiveNext();
			objectSyncPackage.timeDelta = (float)stream.ReceiveNext();
			syncPackages.Add(objectSyncPackage);
		}
	}

	[PunRPC]
	public void RPCA_Collide(Vector2 colForce)
	{
		soundParameterIntensity.intensity = colForce.magnitude;
		SoundManager.Instance.PlayAtPosition(soundBoxImpact, SoundManager.Instance.GetTransform(), base.transform, soundParameterIntensity, soundParameterPitchSemitone);
		GamefeelManager.instance.AddGameFeel(colForce);
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (photonView.IsMine && !(collision.contacts[0].normalImpulse < collisionThreshold) && !(sinceCol < 0.1f))
		{
			sinceCol = 0f;
			photonView.RPC("RPCA_Collide", RpcTarget.All, Mathf.Clamp(collision.contacts[0].normalImpulse, 0f, maxShake) * collision.contacts[0].normal * shakeAmount);
		}
	}

	private void OnPlayerCollision(Vector2 collision, CharacterData player)
	{
		if (!player.view.IsMine || sinceDealDMG < 1f)
		{
			return;
		}
		Vector3 vector = collision * dmgAmount;
		if (!(vector.magnitude < playerColThreshold))
		{
			float num = Mathf.Pow(rig2D.mass / 20000f, 2f);
			float num2 = Mathf.Pow(rig2D.mass / 20000f, 0.5f);
			player.healthHandler.CallTakeDamage(vector * 0.3f * num, player.transform.position);
			player.healthHandler.CallTakeForce(collision * forceAmount * num2, ForceMode2D.Impulse, forceIgnoreMass: false, ignoreBlock: false, vector.magnitude * 0.05f);
			if (player.block.IsBlocking())
			{
				rig2D.velocity *= -1.1f;
				rig2D.angularVelocity *= -1.1f;
			}
			else if (rig2D.mass < 80000f)
			{
				rig2D.velocity *= -0.5f * (20000f / rig2D.mass);
				rig2D.angularVelocity *= -0.5f * (20000f / rig2D.mass);
			}
			sinceDealDMG = 0f;
			photonView.RPC("RPCA_PlayerCollision", RpcTarget.AllViaServer, collision, rig2D.velocity, base.transform.position, player.view.ViewID);
		}
	}

	[PunRPC]
	public void RPCM_RequestCollide(Vector2 collision, Vector2 afterVel, Vector3 position, int playerId)
	{
		photonView.RPC("RPCA_PlayerCollision", RpcTarget.AllViaServer, collision, afterVel, position, playerId);
	}

	[PunRPC]
	private void RPCA_PlayerCollision(Vector2 collision, Vector2 velAfter, Vector3 position, int playerID)
	{
		CharacterData component = PhotonNetwork.GetPhotonView(playerID).GetComponent<CharacterData>();
		base.transform.position = position;
		rig2D.velocity = velAfter;
		sinceDealDMG = 0f;
		StartCoroutine(component.GetComponent<PlayerCollision>().IDoBounce(component.playerVel.velocity));
	}

	public void BulletPush(Vector2 force, Vector2 localPoint, CharacterData asker)
	{
		if (photonView.IsMine)
		{
			rig2D.AddForceAtPosition(force * bulletPushMultiplier, base.transform.TransformPoint(localPoint), ForceMode2D.Impulse);
		}
	}

	[PunRPC]
	public void RPCA_SendForce(Vector2 forceSent, Vector2 sentForcePos)
	{
		rig2D.AddForceAtPosition(forceSent, base.transform.TransformPoint(currentForcePos));
	}

	public Vector3 Push(CharacterData data)
	{
		if (!data.view.IsMine)
		{
			return Vector3.zero;
		}
		sincePushed = 0f;
		Vector2 vector = data.input.direction * 8f;
		Vector2 vector2 = col.bounds.ClosestPoint(data.transform.position);
		float num = Vector2.Angle(vector, vector2 - (Vector2)data.transform.position);
		float num2 = (90f - num) / 90f;
		Vector2 vector3 = TimeHandler.fixedDeltaTime * vector * num2 * speed * 1000f;
		currentForceToSend += vector3;
		currentForcePos = base.transform.InverseTransformPoint(vector2);
		float num3 = Mathf.Clamp((Vector2.Angle(rig2D.velocity, (Vector2)base.transform.position - (Vector2)data.transform.position) - 90f) / 90f, 0f, 1f);
		OnPlayerCollision(rig2D.velocity * num3, data);
		return -vector;
	}

	public void RequestOwnership(CharacterData player)
	{
	}
}
