using UnityEngine;

public class SoundAudioListenerPosition : MonoBehaviour
{
	private float positionZaxis = -50f;

	private AudioListener audioListener;

	private Transform audioListenerTransform;

	private Transform cachedTransform;

	private Vector3 postion;

	private void Awake()
	{
		audioListener = Object.FindObjectOfType<AudioListener>();
		if (audioListener == null)
		{
			Debug.LogError("No AudioListener Found");
		}
		else
		{
			audioListenerTransform = audioListener.transform;
		}
		cachedTransform = base.transform;
	}

	private void Update()
	{
		if (audioListenerTransform != null)
		{
			postion = cachedTransform.position;
			postion.z = positionZaxis;
			audioListenerTransform.position = postion;
		}
	}
}
