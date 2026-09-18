using Photon.Pun;
using Sonigon;
using UnityEngine;

public class DodgeGround : MonoBehaviour
{
	[Header("Sound")]
	public SoundEvent soundSneakyDodgeGroundLoop;

	private bool soundIsPlaying;

	private float soundTimeToStopPlaying = 0.5f;

	private float soundTimeCurrent;

	private Transform spawnedSpawnerTransform;

	[Header("Settings")]
	public float rayLength = 3f;

	public float rayUp = 0.3f;

	public float force;

	public LayerMask mask;

	private MoveTransform move;

	private SpawnedAttack spawned;

	public ParticleSystem p1;

	public ParticleSystem p2;

	public ParticleSystem p3;

	public LineRenderer l1;

	public LineRenderer l2;

	private float c3;

	private void Start()
	{
		spawned = GetComponentInParent<SpawnedAttack>();
		move = GetComponentInParent<MoveTransform>();
		GetComponentInParent<SyncProjectile>().active = true;
		if (spawned != null && spawned.spawner != null)
		{
			spawnedSpawnerTransform = spawned.spawner.transform;
		}
	}

	private void SoundStart()
	{
		if (!soundIsPlaying && spawnedSpawnerTransform != null)
		{
			soundIsPlaying = true;
			SoundManager.Instance.PlayAtPosition(soundSneakyDodgeGroundLoop, spawnedSpawnerTransform, base.transform);
		}
	}

	private void SoundStop()
	{
		if (soundIsPlaying && spawnedSpawnerTransform != null)
		{
			soundIsPlaying = false;
			SoundManager.Instance.StopAtPosition(soundSneakyDodgeGroundLoop, base.transform);
		}
	}

	private void OnDestroy()
	{
		SoundStop();
	}

	private void Update()
	{
		float magnitude = move.velocity.magnitude;
		c3 += TimeHandler.deltaTime;
		l1.enabled = false;
		l2.enabled = false;
		bool flag = false;
		float num = 1f;
		RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position, base.transform.forward + base.transform.right * rayUp, rayLength, mask);
		if ((bool)raycastHit2D && (bool)raycastHit2D.transform && !raycastHit2D.collider.GetComponent<Damagable>() && raycastHit2D.transform.gameObject.layer != 10)
		{
			flag = true;
		}
		bool flag2 = false;
		RaycastHit2D raycastHit2D2 = Physics2D.Raycast(base.transform.position, base.transform.forward + base.transform.right * (0f - rayUp), rayLength, mask);
		if ((bool)raycastHit2D2 && (bool)raycastHit2D2.transform && !raycastHit2D2.collider.GetComponent<Damagable>() && raycastHit2D2.transform.gameObject.layer != 10)
		{
			flag2 = true;
		}
		if (flag && flag2 && raycastHit2D.transform == raycastHit2D2.transform)
		{
			if (raycastHit2D.distance < raycastHit2D2.distance)
			{
				flag2 = false;
			}
			else
			{
				flag = false;
			}
		}
		if (flag)
		{
			move.velocity += (Vector3)raycastHit2D.normal * force * move.velocity.magnitude * num * TimeHandler.deltaTime;
			l1.enabled = true;
			l1.SetPosition(0, base.transform.position);
			l1.SetPosition(1, raycastHit2D.point);
		}
		if (flag2)
		{
			move.velocity += (Vector3)raycastHit2D2.normal * force * move.velocity.magnitude * num * TimeHandler.deltaTime;
			l2.enabled = true;
			l2.SetPosition(0, base.transform.position);
			l2.SetPosition(1, raycastHit2D2.point);
		}
		if (flag || flag2)
		{
			soundTimeCurrent = soundTimeToStopPlaying;
			SoundStart();
		}
		else if (soundIsPlaying)
		{
			soundTimeCurrent -= TimeHandler.deltaTime;
			if (soundTimeCurrent < 0f)
			{
				SoundStop();
			}
		}
		move.velocity = move.velocity.normalized * magnitude;
		if (flag && c3 > 0.05f)
		{
			c3 = 0f;
			p3.Emit(1);
		}
	}
}
