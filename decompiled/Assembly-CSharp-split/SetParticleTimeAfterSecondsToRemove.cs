using UnityEngine;

public class SetParticleTimeAfterSecondsToRemove : MonoBehaviour
{
	private void Start()
	{
		ParticleSystem component = GetComponent<ParticleSystem>();
		component.Stop();
		ParticleSystem.MainModule main = component.main;
		main.duration = GetComponentInParent<RemoveAfterSeconds>().seconds - main.startLifetime.constant;
		component.Play();
	}
}
