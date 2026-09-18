using UnityEngine;

public class MusicVisualizerData : MonoBehaviour
{
	public static float[] Samples = new float[512];

	private AudioSource m_audioSource;

	private void Awake()
	{
		m_audioSource = GetComponent<AudioSource>();
	}

	private void Update()
	{
		m_audioSource.GetSpectrumData(Samples, 0, FFTWindow.Blackman);
	}
}
