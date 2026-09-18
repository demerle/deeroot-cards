using Sonigon;
using UnityEngine;

namespace SoundImplementation;

public class SoundMusicManager : MonoBehaviour
{
	private bool isGoingToDelete;

	private bool useDontDestroyOnLoad;

	private bool debugMusicPlay;

	[Header("Music, press Pause to toggle the volume of the music")]
	public SoundEvent musicMainMenu;

	public SoundEvent musicIngame;

	private bool musicFirstPlay;

	private bool musicMainMenuPlaying;

	private bool musicIngamePlaying;

	private SoundParameterIntensity musicIntensityIngame = new SoundParameterIntensity(1f, UpdateMode.Continuous);

	public SoundEvent ambience;

	private bool ambiencePlaying;

	public static SoundMusicManager Instance { get; private set; }

	public void PlayMainMenu()
	{
		StopAmbience();
		if (!musicMainMenuPlaying)
		{
			musicMainMenuPlaying = true;
			if (SoundVolumeManager.Instance.mainMenuFirstTime)
			{
				SoundVolumeManager.Instance.mainMenuFirstTime = false;
				SoundManager.Instance.PlayMusic(musicMainMenu, true, true, new SoundParameterFadeInLength());
			}
			else
			{
				SoundManager.Instance.PlayMusic(musicMainMenu);
			}
			musicIngamePlaying = false;
			if (debugMusicPlay)
			{
				Debug.LogWarning("MUSIC: PlayMainMenu");
			}
		}
	}

	public void PlayIngame(bool isCard)
	{
		PlayAmbience();
		if (isCard)
		{
			musicIntensityIngame.intensity = 0f;
			if (debugMusicPlay)
			{
				Debug.LogWarning("MUSIC: Ingame Instensity 0");
			}
		}
		else
		{
			musicIntensityIngame.intensity = 1f;
			if (debugMusicPlay)
			{
				Debug.LogWarning("MUSIC: Ingame Instensity 1");
			}
		}
		if (!musicIngamePlaying)
		{
			musicIngamePlaying = true;
			SoundManager.Instance.PlayMusic(musicIngame, true, true, musicIntensityIngame);
			musicMainMenuPlaying = false;
			if (debugMusicPlay)
			{
				Debug.LogWarning("MUSIC: PlayIngame");
			}
		}
	}

	public void StopAllMusic()
	{
		SoundManager.Instance.StopAllMusic();
		musicMainMenuPlaying = false;
		musicIngamePlaying = false;
	}

	private void PlayAmbience()
	{
		if (!ambiencePlaying)
		{
			ambiencePlaying = true;
			SoundManager.Instance.Play(ambience, base.transform);
		}
	}

	private void StopAmbience()
	{
		if (ambiencePlaying)
		{
			ambiencePlaying = false;
			SoundManager.Instance.Stop(ambience, base.transform);
		}
	}

	private void Start()
	{
		if (!musicFirstPlay)
		{
			musicFirstPlay = true;
			PlayMainMenu();
		}
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
