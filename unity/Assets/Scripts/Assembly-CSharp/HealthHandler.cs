using System;
using System.Collections;
using Photon.Pun;
using Sonigon;
using UnityEngine;

public class HealthHandler : Damagable
{
	[Header("Sounds")]
	public SoundEvent soundDie;

	public SoundEvent soundHeal;

	public SoundEvent soundDamagePassive;

	public SoundEvent soundDamageLifeSteal;

	public SoundEvent soundBounce;

	[Header("Settings")]
	public SpriteRenderer hpSprite;

	public GameObject deathEffect;

	public float regeneration;

	private CharacterData data;

	private CodeAnimation anim;

	private Player player;

	private CharacterStatModifiers stats;

	public ParticleSystem healPart;

	private DamageOverTime dot;

	private Vector3 startHealthSpriteScale;

	public float flyingFor;

	private float lastDamaged;

	public Action delayedReviveAction;

	public Action reviveAction;

	[HideInInspector]
	public bool DestroyOnDeath;

	public bool isRespawning;

	public GameObject deathEffectPhoenix;

	private void Awake()
	{
		dot = GetComponent<DamageOverTime>();
		data = GetComponent<CharacterData>();
		anim = GetComponentInChildren<CodeAnimation>();
		player = GetComponent<Player>();
		stats = GetComponent<CharacterStatModifiers>();
	}

	private void Start()
	{
		startHealthSpriteScale = hpSprite.transform.localScale;
	}

	private void Update()
	{
		flyingFor -= TimeHandler.deltaTime;
		if (regeneration > 0f)
		{
			Heal(regeneration * TimeHandler.deltaTime);
		}
	}

	public void Heal(float healAmount)
	{
		if (healAmount != 0f && data.health != data.maxHealth)
		{
			SoundManager.Instance.Play(soundHeal, base.transform);
			data.health += healAmount;
			data.health = Mathf.Clamp(data.health, float.NegativeInfinity, data.maxHealth);
			healPart.Emit((int)Mathf.Clamp(healAmount * 0.2f, 1f, 10f));
		}
	}

	public void CallTakeForce(Vector2 force, ForceMode2D forceMode = ForceMode2D.Impulse, bool forceIgnoreMass = false, bool ignoreBlock = false, float setFlying = 0f)
	{
		if (data.isPlaying && (!data.block.IsBlocking() || ignoreBlock))
		{
			data.view.RPC("RPCA_SendTakeForce", RpcTarget.All, force, (int)forceMode, forceIgnoreMass, true, setFlying);
		}
	}

	[PunRPC]
	public void RPCA_SendTakeForce(Vector2 force, int forceMode, bool forceIgnoreMass = false, bool ignoreBlock = false, float setFlying = 0f)
	{
		TakeForce(force, (ForceMode2D)forceMode, forceIgnoreMass, ignoreBlock, setFlying);
		data.GetComponent<SyncPlayerMovement>().SetDontSyncFor((float)PhotonNetwork.GetPing() * 0.001f + 0.2f);
	}

	[PunRPC]
	public void RPCA_SendForceOverTime(Vector2 force, float time, int forceMode, bool forceIgnoreMass = false, bool ignoreBlock = false)
	{
		StartCoroutine(IForceOverTime(force, time, forceMode, forceIgnoreMass, ignoreBlock));
	}

	private IEnumerator IForceOverTime(Vector2 force, float time, int forceMode, bool forceIgnoreMass = false, bool ignoreBlock = false)
	{
		for (float i = 0f; i < time; i += TimeHandler.deltaTime)
		{
			TakeForce(force, (ForceMode2D)forceMode, forceIgnoreMass, ignoreBlock);
			data.GetComponent<SyncPlayerMovement>().SetDontSyncFor((float)PhotonNetwork.GetPing() * 0.001f + 0.2f);
			yield return new WaitForFixedUpdate();
		}
	}

	[PunRPC]
	public void RPCA_SendForceTowardsPointOverTime(float force, float drag, float clampDistancce, Vector2 point, float time, int forceMode, bool forceIgnoreMass = false, bool ignoreBlock = false)
	{
		StartCoroutine(IForceTowardsPointOverTime(force, drag, clampDistancce, point, time, forceMode, forceIgnoreMass, ignoreBlock));
	}

	private IEnumerator IForceTowardsPointOverTime(float force, float drag, float clampDistancce, Vector2 point, float time, int forceMode, bool forceIgnoreMass = false, bool ignoreBlock = false)
	{
		for (float i = 0f; i < time; i += TimeHandler.fixedDeltaTime)
		{
			Vector2 vector = point - (Vector2)base.transform.position;
			vector = Vector2.ClampMagnitude(vector, clampDistancce);
			Vector2 vector2 = data.playerVel.velocity * (0f - drag) * TimeHandler.fixedDeltaTime;
			TakeForce(force * vector + vector2 * drag * TimeHandler.timeScale, (ForceMode2D)forceMode, forceIgnoreMass, ignoreBlock);
			data.GetComponent<SyncPlayerMovement>().SetDontSyncFor((float)PhotonNetwork.GetPing() * 0.001f + 0.2f);
			yield return new WaitForFixedUpdate();
		}
	}

	public void TakeForce(Vector2 force, ForceMode2D forceMode = ForceMode2D.Impulse, bool forceIgnoreMass = false, bool ignoreBlock = false, float setFlying = 0f)
	{
		if (!data.isPlaying || !data.playerVel.simulated)
		{
			return;
		}
		bool flag = data.block.IsBlocking();
		if (flag && !ignoreBlock)
		{
			return;
		}
		if (!flag)
		{
			if (setFlying > flyingFor && setFlying > 0.25f)
			{
				flyingFor = setFlying;
				SoundManager.Instance.Play(soundBounce, base.transform);
			}
		}
		if (forceIgnoreMass)
		{
			force *= data.playerVel.mass / 100f;
		}
		data.playerVel.AddForce(force, forceMode);
		if (force.y > 0f)
		{
			if (!forceIgnoreMass)
			{
				force.y /= data.playerVel.mass / 100f;
			}
			if (forceMode == ForceMode2D.Force)
			{
				force *= 0.003f;
			}
			data.sinceGrounded -= force.y * 0.003f;
		}
		data.sinceGrounded = Mathf.Clamp(data.sinceGrounded, -0.5f, 100f);
	}

	public override void CallTakeDamage(Vector2 damage, Vector2 position, GameObject damagingWeapon = null, Player damagingPlayer = null, bool lethal = true)
	{
		if (!(damage == Vector2.zero) && !data.block.IsBlocking())
		{
			data.view.RPC("RPCA_SendTakeDamage", RpcTarget.All, damage, position, lethal, (damagingPlayer != null) ? damagingPlayer.playerID : (-1));
		}
	}

	[PunRPC]
	public void RPCA_SendTakeDamage(Vector2 damage, Vector2 position, bool lethal = true, int playerID = -1)
	{
		if (!(damage == Vector2.zero))
		{
			Player playerWithID = PlayerManager.instance.GetPlayerWithID(playerID);
			GameObject damagingWeapon = null;
			if ((bool)playerWithID)
			{
				damagingWeapon = playerWithID.data.weaponHandler.gun.gameObject;
			}
			TakeDamage(damage, position, damagingWeapon, playerWithID, lethal, true);
		}
	}

	public override void TakeDamage(Vector2 damage, Vector2 position, GameObject damagingWeapon = null, Player damagingPlayer = null, bool lethal = true, bool ignoreBlock = false)
	{
		if (!(damage == Vector2.zero))
		{
			TakeDamage(damage, position, Color.white * 0.85f, damagingWeapon, damagingPlayer, lethal, ignoreBlock);
		}
	}

	public override void TakeDamage(Vector2 damage, Vector2 position, Color dmgColor, GameObject damagingWeapon = null, Player damagingPlayer = null, bool lethal = true, bool ignoreBlock = false)
	{
		if (!(damage == Vector2.zero) && data.isPlaying && data.playerVel.simulated && !data.dead && (!data.block.IsBlocking() || ignoreBlock))
		{
			if (dmgColor == Color.black)
			{
				dmgColor = Color.white * 0.85f;
			}
			if (stats.secondsToTakeDamageOver == 0f)
			{
				DoDamage(damage, position, dmgColor, damagingWeapon, damagingPlayer, false, lethal, ignoreBlock);
			}
			else
			{
				TakeDamageOverTime(damage, position, stats.secondsToTakeDamageOver, 0.25f, dmgColor, damagingWeapon, damagingPlayer, lethal);
			}
		}
	}

	public void DoDamage(Vector2 damage, Vector2 position, Color blinkColor, GameObject damagingWeapon = null, Player damagingPlayer = null, bool healthRemoval = false, bool lethal = true, bool ignoreBlock = false)
	{
		if (damage == Vector2.zero || !data.isPlaying || data.dead || (data.block.IsBlocking() && !ignoreBlock) || isRespawning)
		{
			return;
		}
		if ((bool)damagingPlayer)
		{
			damagingPlayer.GetComponent<CharacterStatModifiers>().DealtDamage(damage, damagingPlayer != null && damagingPlayer.transform.root == base.transform, data.player);
		}
		StopAllCoroutines();
		DisplayDamage(blinkColor);
		data.lastSourceOfDamage = damagingPlayer;
		data.health -= damage.magnitude;
		stats.WasDealtDamage(damage, damagingPlayer != null && damagingPlayer.transform.root == base.transform);
		if (!lethal)
		{
			data.health = Mathf.Clamp(data.health, 1f, data.maxHealth);
		}
		if (data.health < 0f && !data.dead)
		{
			if (data.stats.remainingRespawns > 0)
			{
				data.view.RPC("RPCA_Die_Phoenix", RpcTarget.All, damage);
			}
			else
			{
				data.view.RPC("RPCA_Die", RpcTarget.All, damage);
			}
		}
		if (lastDamaged + 0.15f < Time.time && damagingPlayer != null && damagingPlayer.data.stats.lifeSteal != 0f)
		{
			SoundManager.Instance.Play(soundDamageLifeSteal, base.transform);
		}
		lastDamaged = Time.time;
	}

	public void TakeDamageOverTime(Vector2 damage, Vector2 position, float time, float interval, Color color, GameObject damagingWeapon = null, Player damagingPlayer = null, bool lethal = true)
	{
		dot.TakeDamageOverTime(damage, position, time, interval, color, soundDamagePassive, damagingWeapon, damagingPlayer, lethal);
	}

	private void DisplayDamage(Color blinkColor)
	{
		GetComponentInChildren<PlayerSkinHandler>().BlinkColor(blinkColor);
	}

	private IEnumerator DelayReviveAction()
	{
		yield return new WaitForSecondsRealtime(2f);
		Action action = delayedReviveAction;
		if (action != null)
		{
			action();
		}
	}

	public void Revive(bool isFullRevive = true)
	{
		Action action = reviveAction;
		if (action != null)
		{
			action();
		}
		if (base.gameObject.activeInHierarchy)
		{
			StartCoroutine(DelayReviveAction());
		}
		flyingFor = 0f;
		if (isFullRevive)
		{
			data.stats.remainingRespawns = data.stats.respawns;
		}
		data.healthHandler.isRespawning = false;
		data.health = data.maxHealth;
		data.playerVel.velocity = Vector2.zero;
		data.playerVel.angularVelocity = 0f;
		data.stunTime = 0f;
		data.block.ResetCD(false);
		data.weaponHandler.gun.GetComponentInChildren<GunAmmo>().ReloadAmmo(false);
		data.GetComponent<PlayerCollision>().IgnoreWallForFrames(5);
		base.gameObject.SetActive(true);
		if ((bool)deathEffect && data.dead)
		{
			anim.PlayIn();
		}
		data.dead = false;
		hpSprite.color = PlayerSkinBank.GetPlayerSkinColors(player.playerID).color;
		data.stunHandler.StopStun();
		data.silenceHandler.StopSilence();
		GetComponent<CharacterStatModifiers>().slow = 0f;
		GetComponent<CharacterStatModifiers>().slowSlow = 0f;
		GetComponent<CharacterStatModifiers>().fastSlow = 0f;
		GetComponent<WeaponHandler>().isOverHeated = false;
		GetComponent<Block>().sinceBlock = float.PositiveInfinity;
		dot.StopAllCoroutines();
	}

	[PunRPC]
	private void RPCA_Die(Vector2 deathDirection)
	{
		if (data.isPlaying && !data.dead)
		{
			SoundManager.Instance.Play(soundDie, base.transform);
			data.dead = true;
			if (!DestroyOnDeath)
			{
				base.gameObject.SetActive(false);
				GamefeelManager.GameFeel(deathDirection.normalized * 3f);
				UnityEngine.Object.Instantiate(deathEffect, base.transform.position, base.transform.rotation).GetComponent<DeathEffect>().PlayDeath(PlayerSkinBank.GetPlayerSkinColors(player.playerID).color, data.playerVel, deathDirection);
				dot.StopAllCoroutines();
				data.stunHandler.StopStun();
				data.silenceHandler.StopSilence();
				PlayerManager.instance.PlayerDied(player);
			}
			else
			{
				UnityEngine.Object.Destroy(base.transform.root.gameObject);
			}
		}
	}

	[PunRPC]
	private void RPCA_Die_Phoenix(Vector2 deathDirection)
	{
		if (data.isPlaying && !data.dead)
		{
			data.stats.remainingRespawns--;
			isRespawning = true;
			SoundManager.Instance.Play(soundDie, base.transform);
			if (!DestroyOnDeath)
			{
				base.gameObject.SetActive(false);
				GamefeelManager.GameFeel(deathDirection.normalized * 3f);
				UnityEngine.Object.Instantiate(deathEffectPhoenix, base.transform.position, base.transform.rotation).GetComponent<DeathEffect>().PlayDeath(PlayerSkinBank.GetPlayerSkinColors(player.playerID).color, data.playerVel, deathDirection, player.playerID);
				dot.StopAllCoroutines();
				data.stunHandler.StopStun();
				data.silenceHandler.StopSilence();
			}
		}
	}
}
