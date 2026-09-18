using UnityEngine;

public class PlayerSkinParticle : MonoBehaviour
{
	private Color startColor1;

	private Color startColor2;

	private ParticleSystem.MainModule main;

	private ParticleSystem part;

	private ParticleSystem.Particle[] particles;

	private float counter;

	public void Init(int spriteLayerID)
	{
		part = GetComponent<ParticleSystem>();
		part.GetComponent<ParticleSystemRenderer>().sortingLayerID = spriteLayerID;
		main = part.main;
		startColor1 = main.startColor.colorMin;
		startColor2 = main.startColor.colorMax;
		part.Play();
	}

	private void Update()
	{
		counter += TimeHandler.deltaTime;
	}

	private void OnEnable()
	{
		if ((bool)part)
		{
			part.Play();
		}
	}

	public void BlinkColor(Color blinkColor)
	{
		if (!(counter < 0.1f))
		{
			counter = 0f;
			particles = new ParticleSystem.Particle[part.main.maxParticles];
			int num = part.GetParticles(particles);
			for (int i = 0; i < num; i++)
			{
				particles[i].startColor = blinkColor;
			}
			part.SetParticles(particles, num);
		}
	}
}
