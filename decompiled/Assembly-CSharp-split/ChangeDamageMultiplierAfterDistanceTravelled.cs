using UnityEngine;

public class ChangeDamageMultiplierAfterDistanceTravelled : MonoBehaviour
{
	public float muiltiplier = 2f;

	public float distance = 10f;

	private ProjectileHit hit;

	private MoveTransform move;

	private TrailRenderer trail;

	private void Awake()
	{
		trail = GetComponentInChildren<TrailRenderer>();
	}

	private void Start()
	{
		hit = GetComponent<ProjectileHit>();
		move = GetComponent<MoveTransform>();
	}

	private void Update()
	{
		if (move.distanceTravelled > distance)
		{
			hit.damage *= muiltiplier;
			hit.shake *= muiltiplier;
			if ((bool)trail)
			{
				trail.widthMultiplier *= 2f;
			}
			Object.Destroy(this);
		}
	}
}
