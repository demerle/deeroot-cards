using System.Collections.Generic;
using UnityEngine;

public class PlayerAPI : MonoBehaviour
{
	public Player player;

	public CharacterData data;

	private GeneralInput input;

	private bool movedThisFrame;

	private bool attackedThisFrame;

	private bool blockedThisFrame;

	private void Awake()
	{
		player = GetComponent<Player>();
		data = GetComponent<CharacterData>();
		input = GetComponent<GeneralInput>();
	}

	public void Move(Vector2 direction)
	{
		direction = Vector2.ClampMagnitude(direction, 1f);
		movedThisFrame = true;
		data.input.direction = direction;
	}

	public void Jump()
	{
		data.jump.Jump();
	}

	public void Attack()
	{
		attackedThisFrame = true;
		data.input.shootWasPressed = true;
		data.input.shootIsPressed = true;
	}

	public void Block()
	{
		data.input.shieldWasPressed = true;
	}

	public void SetAimDirection(Vector2 direction)
	{
		data.input.aimDirection = direction;
	}

	public void AimForOtherPlayer()
	{
		Player otherPlayer = PlayerManager.instance.GetOtherPlayer(player);
		if ((bool)otherPlayer)
		{
			data.input.aimDirection = otherPlayer.transform.position - base.transform.position;
		}
	}

	public Vector2 TowardsOtherPlayer()
	{
		if (PlayerManager.instance.players.Count < 2)
		{
			return Vector2.zero;
		}
		return PlayerManager.instance.GetOtherPlayer(player).transform.position - base.transform.position;
	}

	public RaycastHit2D RayCastDirection(Vector2 direction, float distance)
	{
		return Physics2D.Raycast(base.transform.position, direction, distance);
	}

	public bool CheckGroundBelow(Vector2 pos, float range)
	{
		return data.ThereIsGroundBelow(pos, range);
	}

	public bool CanBlock()
	{
		return !data.block.IsOnCD();
	}

	public Player GetOtherPlayer()
	{
		return PlayerManager.instance.GetOtherPlayer(player);
	}

	public Vector3 OtherPlayerPosition()
	{
		Player otherPlayer = PlayerManager.instance.GetOtherPlayer(player);
		if ((bool)otherPlayer)
		{
			return otherPlayer.transform.position;
		}
		return Vector3.zero;
	}

	public Vector3 PlayerPosition()
	{
		return base.transform.position;
	}

	public List<BulletWrapper> GetAllBullets()
	{
		List<BulletWrapper> list = new List<BulletWrapper>();
		ProjectileHit[] array = Object.FindObjectsOfType<ProjectileHit>();
		for (int i = 0; i < array.Length; i++)
		{
			BulletWrapper bulletWrapper = new BulletWrapper();
			bulletWrapper.projectileHit = array[i].GetComponent<ProjectileHit>();
			bulletWrapper.projectileMovement = array[i].GetComponent<MoveTransform>();
			bulletWrapper.damage = bulletWrapper.projectileHit.damage;
			bulletWrapper.velocity = bulletWrapper.projectileMovement.velocity;
			list.Add(bulletWrapper);
		}
		return list;
	}

	public SpawnedAttack[] GetAllSpawnedAttacks()
	{
		return Object.FindObjectsOfType<SpawnedAttack>();
	}

	public bool CanShoot()
	{
		return player.data.weaponHandler.gun.IsReady();
	}

	public BulletWrapper GetMyBullet()
	{
		BulletWrapper bulletWrapper = new BulletWrapper();
		GameObject objectToSpawn = player.data.weaponHandler.gun.projectiles[0].objectToSpawn;
		MoveTransform component = objectToSpawn.GetComponent<MoveTransform>();
		ProjectileHit component2 = objectToSpawn.GetComponent<ProjectileHit>();
		bulletWrapper.projectileMovement = component;
		bulletWrapper.projectileHit = component2;
		bulletWrapper.damage = component2.damage;
		bulletWrapper.velocity = player.data.aimDirection.normalized * component.localForce.magnitude + component.worldForce;
		return bulletWrapper;
	}

	private void Update()
	{
		if (blockedThisFrame)
		{
			blockedThisFrame = false;
		}
		else
		{
			data.input.shieldWasPressed = false;
		}
		if (movedThisFrame)
		{
			movedThisFrame = false;
		}
		else
		{
			data.input.direction = Vector3.zero;
		}
		if (attackedThisFrame)
		{
			attackedThisFrame = false;
			return;
		}
		data.input.shootWasPressed = false;
		data.input.shootIsPressed = false;
	}
}
