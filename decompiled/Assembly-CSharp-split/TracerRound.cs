using UnityEngine;

public class TracerRound : MonoBehaviour
{
	public GameObject bullet;

	public GameObject bulletSpawnPos;

	private TracerTarget target;

	private MoveTransform move;

	private bool done;

	private void Start()
	{
		move = GetComponent<MoveTransform>();
		bullet = Object.Instantiate(bullet, bulletSpawnPos.transform.position, bulletSpawnPos.transform.rotation);
		bullet.GetComponent<ProjectileHit>().damage *= base.transform.localScale.x;
		GetComponentInParent<SpawnedAttack>().CopySpawnedAttackTo(bullet);
		target = bullet.GetComponent<TracerTarget>();
		GetComponentInParent<ProjectileHit>().AddHitActionWithData(Hit);
		bullet.GetComponentInParent<ProjectileHit>().AddHitActionWithData(Hit);
	}

	public void Hit(HitInfo hit)
	{
		if ((bool)hit.transform.root.GetComponent<Player>())
		{
			base.transform.SetParent(null);
			base.gameObject.AddComponent<RemoveAfterSeconds>().seconds = 10f;
			base.gameObject.AddComponent<FollowTransform>().target = hit.transform;
		}
	}

	private void Update()
	{
		if (target != null)
		{
			target.SetPos(base.transform.position, Vector3.Cross(Vector3.forward, base.transform.forward), move);
		}
		if (bullet == null && !done)
		{
			done = true;
			GetComponentInChildren<ParticleSystem>().Stop();
		}
	}
}
