using System.Collections;
using UnityEngine;

public class RadarShot : MonoBehaviour
{
	private WeaponHandler wh;

	private Player player;

	public ParticleSystem[] boops;

	public float range = 12f;

	private void Start()
	{
		wh = GetComponentInParent<WeaponHandler>();
		player = GetComponentInParent<Player>();
	}

	public void Go()
	{
		Player closestPlayerInTeam = PlayerManager.instance.GetClosestPlayerInTeam(base.transform.position, PlayerManager.instance.GetOtherTeam(player.teamID), needVision: true);
		if ((bool)closestPlayerInTeam && Vector2.Distance(player.transform.position, closestPlayerInTeam.transform.position) < range)
		{
			StartCoroutine(FollowTarget(closestPlayerInTeam));
			if (player.data.view.IsMine)
			{
				StartCoroutine(ShootAttacks(closestPlayerInTeam, GetComponent<AttackLevel>().attackLevel));
			}
		}
	}

	private IEnumerator ShootAttacks(Player target, int shots)
	{
		for (int i = 0; i < shots; i++)
		{
			yield return new WaitForSeconds(0.1f);
			wh.gun.forceShootDir = wh.gun.GetRangeCompensation(Vector3.Distance(target.transform.position, player.transform.position)) * Vector3.up + target.transform.position - player.transform.position;
			wh.gun.Attack(0f, forceAttack: true, 1f, 1f, useAmmo: false);
			wh.gun.forceShootDir = Vector3.zero;
		}
	}

	private IEnumerator FollowTarget(Player target)
	{
		for (int i = 0; i < boops.Length; i++)
		{
			boops[i].Play();
		}
		float c = 0f;
		while (c < 1f)
		{
			c += TimeHandler.deltaTime;
			for (int j = 0; j < boops.Length; j++)
			{
				boops[j].transform.position = target.transform.position;
			}
			yield return null;
		}
	}
}
