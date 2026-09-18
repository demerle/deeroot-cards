using System.Collections;
using UnityEngine;

public class GeneralShooter : MonoBehaviour
{
	private Gun gun;

	[HideInInspector]
	public float charge;

	public Transform[] shooters;

	private void Start()
	{
		gun = GetComponent<Gun>();
	}

	public void Attack()
	{
		if (base.enabled)
		{
			StartCoroutine(DoAttack());
		}
	}

	private IEnumerator DoAttack()
	{
		int shotsToMake = Mathf.Clamp(Mathf.RoundToInt(0.5f / gun.attackSpeed), 1, 5);
		for (int i = 0; i < shotsToMake; i++)
		{
			for (int j = 0; j < shooters.Length; j++)
			{
				gun.transform.position = shooters[j].position;
				gun.transform.rotation = shooters[j].rotation;
				gun.Attack(gun.currentCharge, forceAttack: true);
			}
			if (shotsToMake > i)
			{
				yield return new WaitForSeconds(0.1f);
			}
		}
	}
}
