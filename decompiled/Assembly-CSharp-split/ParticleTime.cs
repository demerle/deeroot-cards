using UnityEngine;

public class ParticleTime : MonoBehaviour
{
	private float startTime = 1f;

	private ParticleSystem.MainModule main;

	private void Start()
	{
		main = GetComponentInParent<ParticleSystem>().main;
		startTime = main.simulationSpeed;
	}

	private void Update()
	{
		main.simulationSpeed = startTime * TimeHandler.timeScale;
	}
}
