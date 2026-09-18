using Sonigon;
using UnityEngine;

namespace SoundImplementation;

public class SoundEnvironmentRigidbody : MonoBehaviour
{
	public SoundEvent soundMoveLoop;

	private Rigidbody2D cachedRigidbody2D;

	private bool soundIsPlaying;

	private SoundParameterIntensity parameterIntensity = new SoundParameterIntensity(0f, UpdateMode.Continuous);

	private void Awake()
	{
		cachedRigidbody2D = GetComponent<Rigidbody2D>();
	}

	private void FixedUpdate()
	{
		if (cachedRigidbody2D != null && soundMoveLoop != null)
		{
			if (!soundIsPlaying)
			{
				soundIsPlaying = true;
				SoundManager.Instance.Play(soundMoveLoop, base.transform, parameterIntensity);
			}
			parameterIntensity.intensity = cachedRigidbody2D.velocity.magnitude;
		}
	}
}
