using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;

public class DamagableEvent : Damagable
{
	public bool networkedDamage;

	public bool disabled;

	[HideInInspector]
	public bool dead;

	public float currentHP = 100f;

	public float regenDelay = 1f;

	public float regenPerSecond;

	[HideInInspector]
	public float maxHP = 100f;

	public UnityEvent damageEvent;

	public UnityEvent deathEvent;

	private float sinceDamage = 1f;

	[HideInInspector]
	public Player lastPlayer;

	[HideInInspector]
	public GameObject lastWeapon;

	private PhotonView view;

	public Action<Vector2> DieAction;

	private void Start()
	{
		view = GetComponent<PhotonView>();
		maxHP = currentHP;
	}

	public override void TakeDamage(Vector2 damage, Vector2 damagePosition, GameObject damagingWeapon = null, Player damagingPlayer = null, bool lethal = true, bool ignoreBlock = false)
	{
		if (disabled || dead)
		{
			return;
		}
		if (networkedDamage)
		{
			if ((bool)damagingPlayer)
			{
				if (!damagingPlayer.data.view.IsMine)
				{
					return;
				}
				view.RPC("RPCA_TakeDamage", RpcTarget.Others, damage, damagePosition);
			}
			else
			{
				if (!view.IsMine)
				{
					return;
				}
				view.RPC("RPCA_TakeDamage", RpcTarget.Others, damage, damagePosition);
			}
		}
		DoDamage(damage, damagePosition, damagingWeapon, damagingPlayer, lethal, ignoreBlock);
	}

	[PunRPC]
	public void RPCA_TakeDamage(Vector2 damage, Vector2 damagePosition)
	{
		DoDamage(damage, damagePosition);
	}

	private void DoDamage(Vector2 damage, Vector2 damagePosition, GameObject damagingWeapon = null, Player damagingPlayer = null, bool lethal = true, bool ignoreBlock = false)
	{
		if ((bool)damagingPlayer)
		{
			lastPlayer = damagingPlayer;
		}
		if ((bool)damagingWeapon)
		{
			lastWeapon = damagingWeapon;
		}
		sinceDamage = 0f;
		currentHP -= damage.magnitude;
		if (currentHP <= 0f)
		{
			Die(damage);
		}
		else
		{
			damageEvent.Invoke();
		}
	}

	private void Die(Vector2 damage = default(Vector2))
	{
		if (!dead && !disabled)
		{
			DieAction?.Invoke(damage);
			deathEvent.Invoke();
			dead = true;
		}
	}

	private void Update()
	{
		if (!dead && !disabled)
		{
			sinceDamage += TimeHandler.deltaTime;
			if (sinceDamage > regenDelay && currentHP < maxHP)
			{
				currentHP += regenPerSecond * TimeHandler.deltaTime;
				currentHP = Mathf.Clamp(currentHP, float.NegativeInfinity, maxHP);
			}
		}
	}

	public override void TakeDamage(Vector2 damage, Vector2 damagePosition, Color dmgColor, GameObject damagingWeapon = null, Player damagingPlayer = null, bool lethal = true, bool ignoreBlock = false)
	{
		TakeDamage(damage, damagePosition, damagingWeapon, damagingPlayer, lethal);
	}

	public override void CallTakeDamage(Vector2 damage, Vector2 damagePosition, GameObject damagingWeapon = null, Player damagingPlayer = null, bool lethal = true)
	{
		TakeDamage(damage, damagePosition, Color.red, damagingWeapon, damagingPlayer, lethal);
	}
}
