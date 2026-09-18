using UnityEngine;
using UnityEngine.Audio;

namespace SoundImplementation;

public class SoundVolumeManager : MonoBehaviour
{
	public bool mainMenuFirstTime = true;

	private bool isGoingToDelete;

	private bool useDontDestroyOnLoad = true;

	private bool debugInstanceDestroyed = true;

	public AudioMixer audioMixer;

	private float mixerLowestVolDb = -60f;

	private string masterName = "MasterPrivateVol";

	private float masterVolOffsetDb = 16f;

	private string musName = "MUSVol";

	private float musVolOffsetDb = 10f;

	private string sfxName = "SFXVol";

	private float sfxVolOffsetDb = 10f;

	public static SoundVolumeManager Instance { get; private set; }

	public void SetAudioMixerVolumes(float masterVol, float musicVol, float sfxVol)
	{
		masterVol = (1f - masterVol) * mixerLowestVolDb;
		musicVol = (1f - musicVol) * mixerLowestVolDb;
		sfxVol = (1f - sfxVol) * mixerLowestVolDb;
		masterVol = ((!(masterVol <= mixerLowestVolDb)) ? (masterVol + masterVolOffsetDb) : (-80f));
		musicVol = ((!(musicVol <= mixerLowestVolDb)) ? (musicVol + musVolOffsetDb) : (-80f));
		sfxVol = ((!(sfxVol <= mixerLowestVolDb)) ? (sfxVol + sfxVolOffsetDb) : (-80f));
		audioMixer.SetFloat(masterName, masterVol);
		audioMixer.SetFloat(musName, musicVol);
		audioMixer.SetFloat(sfxName, sfxVol);
	}

	private void Awake()
	{
		InstanceCheck();
		if (useDontDestroyOnLoad && !isGoingToDelete)
		{
			Object.DontDestroyOnLoad(base.gameObject);
		}
	}

	private void InstanceCheck()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			isGoingToDelete = true;
			if (Application.isPlaying)
			{
				Object.Destroy(base.gameObject);
			}
		}
	}
}
