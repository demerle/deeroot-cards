using UnityEngine;

public class SetTeamColorSpecific : MonoBehaviour
{
	public Color[] colors;

	private void Start()
	{
		Player player = GetComponentInParent<Player>();
		if (!player)
		{
			player = GetComponentInParent<SpawnedAttack>().spawner;
		}
		ParticleSystem component = GetComponent<ParticleSystem>();
		if ((bool)component)
		{
			_ = component.main;
			component.startColor = colors[player.playerID];
		}
		LineRenderer component2 = GetComponent<LineRenderer>();
		if ((bool)component2)
		{
			component2.startColor = colors[player.playerID];
			component2.endColor = colors[player.playerID];
		}
	}
}
