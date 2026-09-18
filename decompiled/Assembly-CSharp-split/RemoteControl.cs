using Photon.Pun;
using Sonigon;
using SoundImplementation;
using UnityEngine;

public class RemoteControl : MonoBehaviour
{
	[Header("Sound")]
	public SoundEvent soundRemoteSteeringLoop;

	private bool soundIsPlaying;

	[Header("Settings")]
	public bool snap;

	public float rotateSpeed = 1f;

	private SpawnedAttack spawned;

	private MoveTransform move;

	private float startVelocity;

	private bool isOn;

	private ProjectileHit hit;

	private ParticleSystem part;

	public ParticleSystem boopPart;

	private float c;

	private PhotonView view;

	private ChildRPC childRPC;

	private void OnDestroy()
	{
		if (soundRemoteSteeringLoop != null && spawned != null && spawned.spawner != null && soundIsPlaying)
		{
			soundIsPlaying = false;
			SoundStaticRemoteControl.remoteControl.AddNumberOf(spawned.spawner.transform, -1);
			if (SoundStaticRemoteControl.remoteControl.GetNumberOf(spawned.spawner.transform) <= 0)
			{
				SoundManager.Instance.Stop(soundRemoteSteeringLoop, spawned.spawner.transform);
			}
		}
	}

	private void Start()
	{
		childRPC = GetComponentInParent<ChildRPC>();
		view = GetComponentInParent<PhotonView>();
		hit = GetComponentInParent<ProjectileHit>();
		move = GetComponentInParent<MoveTransform>();
		spawned = GetComponentInParent<SpawnedAttack>();
		startVelocity = move.velocity.magnitude;
		part = GetComponentInChildren<ParticleSystem>();
		GetComponentInParent<SyncProjectile>().active = true;
		if (soundRemoteSteeringLoop != null && spawned != null && spawned.spawner != null && !soundIsPlaying)
		{
			soundIsPlaying = true;
			if (SoundStaticRemoteControl.remoteControl.GetNumberOf(spawned.spawner.transform) <= 0)
			{
				SoundManager.Instance.Play(soundRemoteSteeringLoop, spawned.spawner.transform);
			}
			SoundStaticRemoteControl.remoteControl.AddNumberOf(spawned.spawner.transform, 1);
		}
	}

	public void ToggleOn(bool isOn)
	{
	}

	private void Update()
	{
		if (!view.IsMine)
		{
			return;
		}
		Vector3 zero = Vector3.zero;
		if (spawned.spawner.data.playerActions.Device != null)
		{
			zero = spawned.spawner.data.input.aimDirection;
		}
		else
		{
			zero = MainCam.instance.cam.ScreenToWorldPoint(Input.mousePosition) - base.transform.position;
			zero.z = 0f;
			zero.Normalize();
		}
		zero += Vector3.Cross(Vector3.forward, zero) * move.selectedSpread;
		c += TimeHandler.deltaTime;
		if (snap)
		{
			if (spawned.spawner.data.block.blockedThisFrame)
			{
				part.Play();
				move.velocity *= -1f;
				base.enabled = false;
			}
		}
		else if (zero.magnitude > 0.2f && hit.sinceReflect > 2f)
		{
			move.velocity = Vector3.RotateTowards(move.velocity, zero.normalized * startVelocity, rotateSpeed * TimeHandler.deltaTime, rotateSpeed * TimeHandler.deltaTime * 10f);
			if (c > 0.1f)
			{
				boopPart.transform.parent.rotation = Quaternion.LookRotation(zero);
				boopPart?.Emit(1);
				c = 0f;
			}
			if (!isOn)
			{
				move.simulateGravity++;
			}
			isOn = true;
		}
		else
		{
			if (isOn)
			{
				move.simulateGravity--;
			}
			isOn = false;
		}
	}
}
