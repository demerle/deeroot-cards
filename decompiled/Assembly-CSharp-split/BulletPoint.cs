using System;
using UnityEngine;

public class BulletPoint : MonoBehaviour
{
	public LayerMask mask;

	private Gun gun;

	private MoveTransform move;

	private ProjectileHit hit;

	private float counter = 5f;

	private void Start()
	{
		gun = GetComponent<Gun>();
		Gun obj = gun;
		obj.ShootPojectileAction = (Action<GameObject>)Delegate.Combine(obj.ShootPojectileAction, new Action<GameObject>(Fire));
	}

	private void Attack()
	{
		gun.Attack(1f, forceAttack: true);
	}

	private void Fire(GameObject projectile)
	{
		move = projectile.GetComponent<MoveTransform>();
	}

	private void Update()
	{
		if ((bool)move)
		{
			counter = 0f;
			Vector3 vector = base.transform.position - base.transform.root.position;
			RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.root.position, vector, vector.magnitude, mask, -10000f);
			Vector3 vector2 = base.transform.position;
			if (raycastHit2D.transform != null)
			{
				vector2 = raycastHit2D.point - (Vector2)vector.normalized * 0.1f;
			}
			move.velocity = Vector2.Lerp(move.velocity, (vector2 - move.transform.position) * 50f, TimeHandler.deltaTime * 50f);
		}
		else
		{
			counter += TimeHandler.deltaTime;
			if (counter > 2f)
			{
				Attack();
			}
		}
	}
}
