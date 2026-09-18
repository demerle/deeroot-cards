using UnityEngine;

public class RescaleFromDamagePart : MonoBehaviour
{
	private ProjectileHit hit;

	private float startSize;

	private ParticleSystem part;

	private void Start()
	{
		part = GetComponent<ParticleSystem>();
		startSize = part.startSize;
	}

	public void Rescale()
	{
		if (!hit)
		{
			hit = GetComponentInParent<ProjectileHit>();
		}
		if ((bool)hit)
		{
			part.startSize = startSize * (hit.damage / 55f);
		}
	}
}
