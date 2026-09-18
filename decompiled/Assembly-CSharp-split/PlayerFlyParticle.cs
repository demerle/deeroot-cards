using UnityEngine;

public class PlayerFlyParticle : MonoBehaviour
{
	private ParticleSystem part;

	private HealthHandler health;

	private void Start()
	{
		part = GetComponent<ParticleSystem>();
		health = GetComponentInParent<HealthHandler>();
	}

	private void Update()
	{
		if (part.isPlaying)
		{
			if (health.flyingFor < 0f)
			{
				part.Stop();
			}
		}
		else if (health.flyingFor > 0f)
		{
			part.Play();
		}
	}
}
