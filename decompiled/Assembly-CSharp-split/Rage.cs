using UnityEngine;

public class Rage : MonoBehaviour
{
	private Player player;

	private AttackLevel level;

	public AnimationCurve curve;

	public AnimationCurve partCurve;

	private ParticleSystem.EmissionModule part;

	private void Start()
	{
		player = GetComponentInParent<Player>();
		level = GetComponentInParent<AttackLevel>();
		part = GetComponentInChildren<ParticleSystem>().emission;
		player.data.SetWobbleObjectChild(GetComponentInChildren<ParticleSystem>().transform);
	}

	private void Update()
	{
		float healthPercentage = player.data.HealthPercentage;
		player.data.stats.rageSpeed = Mathf.Pow(curve.Evaluate(healthPercentage), level.LevelScale());
		part.rateOverTime = partCurve.Evaluate(player.data.stats.rageSpeed);
	}
}
