using System;
using UnityEngine;
using UnityEngine.Events;

public class AttackTrigger : MonoBehaviour
{
	public bool triggerOnEveryShot = true;

	public UnityEvent triggerEvent;

	private CharacterData data;

	private void Start()
	{
		data = GetComponentInParent<CharacterData>();
		Gun gun = data.weaponHandler.gun;
		gun.ShootPojectileAction = (Action<GameObject>)Delegate.Combine(gun.ShootPojectileAction, new Action<GameObject>(Shoot));
		data.weaponHandler.gun.AddAttackAction(Attack);
	}

	private void OnDestroy()
	{
		Gun gun = data.weaponHandler.gun;
		gun.ShootPojectileAction = (Action<GameObject>)Delegate.Remove(gun.ShootPojectileAction, new Action<GameObject>(Shoot));
		data.weaponHandler.gun.RemoveAttackAction(Attack);
	}

	public void Attack()
	{
		if (!triggerOnEveryShot)
		{
			triggerEvent.Invoke();
		}
	}

	public void Shoot(GameObject projectile)
	{
		if (triggerOnEveryShot)
		{
			triggerEvent.Invoke();
		}
	}
}
