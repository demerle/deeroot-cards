using UnityEngine;

public class Holding : MonoBehaviour
{
	public float force;

	public float drag;

	public Holdable holdable;

	private Transform handPos;

	private PlayerVelocity rig;

	private GeneralInput input;

	private CharacterData data;

	private Player player;

	private Gun gun;

	private bool hasSpawnedGun;

	public void Awake()
	{
		if (hasSpawnedGun)
		{
			Object.Destroy(holdable.gameObject);
		}
		hasSpawnedGun = true;
		holdable = Object.Instantiate(holdable, base.transform.position, Quaternion.identity);
		player = GetComponent<Player>();
		holdable.GetComponent<Holdable>().holder = player.data;
		handPos = GetComponentInChildren<HandPos>().transform;
		rig = GetComponent<PlayerVelocity>();
		input = GetComponent<GeneralInput>();
		data = GetComponent<CharacterData>();
		if ((bool)holdable)
		{
			gun = holdable.GetComponent<Gun>();
			GetComponentInChildren<WeaponHandler>().gun = holdable.GetComponent<Gun>();
		}
	}

	private void Start()
	{
		if ((bool)holdable)
		{
			holdable.SetTeamColors(PlayerSkinBank.GetPlayerSkinColors(player.playerID), player);
		}
	}

	private void FixedUpdate()
	{
		if ((bool)holdable && (bool)holdable.rig)
		{
			holdable.rig.AddForce((handPos.transform.position + (Vector3)rig.velocity * 0.04f - holdable.transform.position) * force * holdable.rig.mass, ForceMode2D.Force);
			holdable.rig.AddForce(holdable.rig.velocity * (0f - drag) * holdable.rig.mass, ForceMode2D.Force);
			holdable.rig.transform.rotation = Quaternion.LookRotation(Vector3.forward, handPos.transform.forward);
		}
	}

	private void Update()
	{
	}

	private void OnDestroy()
	{
		if ((bool)holdable)
		{
			Object.Destroy(holdable.gameObject);
		}
	}
}
