using UnityEngine;

public class PowerUps : MonoBehaviour
{
	public GameObject weapon;

	public float damage = 1f;

	public float knockback = 1f;

	public float attackSpeed = 1f;

	public float projectileSpeed = 1f;

	public float spread;

	public float evenSpread;

	public float gravity = 1f;

	public int projectiles = 1;

	public int reflects;

	public int smartBounce;

	public int bulletPortal;

	public int randomBounces;

	public int bursts = 1;

	public float lifeSteal;

	public float projectileSize;

	public float damageAfterDistanceMultiplier = 1f;

	public float distancceForDamageAfterDistanceMultiplier = 5f;

	public float timeToReachFullMovementMultiplier;

	public ObjectsToSpawn[] objectsToSpawn;

	public bool waveMovement;

	public bool teleport;

	public bool spawnSkelletonSquare;

	public float explodeNearEnemyRange;

	public float explodeNearEnemyDamage;

	public float hitMovementMultiplier = 1f;

	private void Start()
	{
		EffectWeapon(weapon);
	}

	private void EffectWeapon(GameObject weapon)
	{
		weapon.GetComponent<Gun>();
	}
}
