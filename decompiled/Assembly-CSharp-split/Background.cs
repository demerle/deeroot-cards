using UnityEngine;

public class Background : MonoBehaviour
{
	private ParticleSystem[] parts;

	private bool hasBeenInitiated;

	private void Init()
	{
		parts = GetComponentsInChildren<ParticleSystem>();
		hasBeenInitiated = true;
	}

	public void ToggleBackground(bool on)
	{
		if (!hasBeenInitiated)
		{
			Init();
		}
		for (int i = 0; i < parts.Length; i++)
		{
			if (on)
			{
				parts[i].Play();
			}
			else
			{
				parts[i].Stop();
			}
		}
	}
}
