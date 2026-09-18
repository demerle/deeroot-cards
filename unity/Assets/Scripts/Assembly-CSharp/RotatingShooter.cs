using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

public class RotatingShooter : MonoBehaviour
{
	private Gun gun;

	[HideInInspector]
	public float charge;

	public int bulletsToFire = 10;

	private int currentBulletsToFire = 10;

	private float degreesPerBullet;

	[FoldoutGroup("Weird settings", 0)]
	public bool destroyAfterAttack = true;

	[FoldoutGroup("Weird settings", 0)]
	public bool disableTrailRenderer = true;

	private AttackLevel level;

	private void Start()
	{
		level = GetComponentInParent<AttackLevel>();
		gun = GetComponent<Gun>();
		if (disableTrailRenderer)
		{
			base.transform.root.GetComponentInChildren<TrailRenderer>(true).enabled = false;
		}
	}

	public void Attack()
	{
		currentBulletsToFire = bulletsToFire;
		if ((bool)level)
		{
			currentBulletsToFire = bulletsToFire * level.attackLevel;
		}
		degreesPerBullet = 360f / (float)currentBulletsToFire;
		StartCoroutine(RotateAndShoot());
	}

	private IEnumerator RotateAndShoot()
	{
		int shotsToMake = Mathf.Clamp(Mathf.RoundToInt(0.5f / gun.attackSpeed), 1, 5);
		for (int i = 0; i < shotsToMake; i++)
		{
			for (int num = currentBulletsToFire; num > 0; num--)
			{
				base.transform.localEulerAngles = new Vector3(0f, (float)num * degreesPerBullet, 0f);
				gun.Attack(gun.currentCharge, true);
			}
			if (shotsToMake > i)
			{
				yield return new WaitForSeconds(0.1f);
			}
		}
		base.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
		yield return null;
		if (destroyAfterAttack)
		{
			Object.Destroy(base.transform.root.gameObject);
		}
	}
}
