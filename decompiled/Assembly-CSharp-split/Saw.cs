using UnityEngine;

public class Saw : MonoBehaviour
{
	public Player owner;

	public float range = 3f;

	public float damage = 10f;

	public float force;

	public float shake = 1f;

	public ParticleSystem[] parts;

	public Transform sparkTransform;

	private Vector3 forceDir;

	private void Start()
	{
		owner = base.transform.root.GetComponent<SpawnedAttack>().spawner;
	}

	private void Update()
	{
		Player player = null;
		for (int i = 0; i < PlayerManager.instance.players.Count; i++)
		{
			Player player2 = PlayerManager.instance.players[i];
			if (player2 != owner && Vector3.Distance(player2.transform.position, base.transform.transform.position) < range * base.transform.localScale.x)
			{
				player = player2;
			}
		}
		if ((bool)player && PlayerManager.instance.CanSeePlayer(base.transform.position, player).canSee)
		{
			Vector3 normalized = (player.transform.position - base.transform.position).normalized;
			if (damage != 0f)
			{
				player.data.healthHandler.TakeDamage(TimeHandler.deltaTime * damage * normalized, base.transform.position, null, owner);
			}
			if (force != 0f)
			{
				float num = Mathf.Clamp(1f - Vector2.Distance(base.transform.position, player.transform.position) / range, 0f, 1f);
				ForceMultiplier component = player.GetComponent<ForceMultiplier>();
				if ((bool)component)
				{
					num *= component.multiplier;
				}
				forceDir = normalized;
				forceDir.y *= 0.5f;
				player.data.playerVel.AddForce(forceDir * base.transform.localScale.x * num * TimeHandler.deltaTime * force, ForceMode2D.Force);
				player.data.healthHandler.TakeForce(forceDir * num * 0.0005f * TimeHandler.deltaTime * force);
			}
			for (int j = 0; j < parts.Length; j++)
			{
				if (!parts[j].isPlaying)
				{
					parts[j].Play();
				}
			}
			if ((bool)sparkTransform)
			{
				sparkTransform.transform.position = player.transform.position;
				if (normalized != Vector3.zero)
				{
					sparkTransform.rotation = Quaternion.LookRotation(normalized);
				}
			}
			GamefeelManager.GameFeel((normalized + Random.onUnitSphere).normalized * shake * TimeHandler.deltaTime * 20f);
			return;
		}
		for (int k = 0; k < parts.Length; k++)
		{
			if (parts[k].isPlaying)
			{
				parts[k].Stop();
			}
		}
	}
}
