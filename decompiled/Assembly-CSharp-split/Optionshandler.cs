using SoundImplementation;
using UnityEngine;

public class Optionshandler : MonoBehaviour
{
	public enum FullScreenOption
	{
		FullScreen,
		WindowedFullScreen,
		MaximizedWindow,
		Windowed
	}

	public static Resolution resolution;

	public static float vol_Master;

	public static float vol_Sfx;

	public static float vol_Music;

	public static FullScreenOption fullScreen;

	public static bool characterPattrens;

	public static bool mapPatterns;

	public static bool vSync;

	public static bool leftStickAim;

	public static bool lockMouse;

	public static bool showCardStatNumbers;

	public static bool lockStick;

	public static Optionshandler instance;

	private void Start()
	{
		instance = this;
		LoadOptions();
		ApplyOptions();
	}

	private void LoadOptions()
	{
		vol_Master = PlayerPrefs.GetFloat("Vol_Master", 1f);
		vol_Sfx = PlayerPrefs.GetFloat("vol_Sfx", 1f);
		vol_Music = PlayerPrefs.GetFloat("vol_Music", 1f);
		resolution.width = PlayerPrefs.GetInt("res_X", 0);
		resolution.height = PlayerPrefs.GetInt("res_Y", 0);
		fullScreen = (FullScreenOption)PlayerPrefs.GetInt("fullScreen", 1);
		characterPattrens = GetBool(PlayerPrefs.GetInt("characterPattrens", 1));
		mapPatterns = GetBool(PlayerPrefs.GetInt("mapPatterns", 1));
		leftStickAim = GetBool(PlayerPrefs.GetInt("leftStickAim", 1));
		vSync = GetBool(PlayerPrefs.GetInt("vSync", 0));
		lockStick = GetBool(PlayerPrefs.GetInt("lockStick", 0));
		lockMouse = GetBool(PlayerPrefs.GetInt("lockMouse", 0));
		showCardStatNumbers = GetBool(PlayerPrefs.GetInt("showCardStatNumbers", 0));
		if (resolution.height == 0 || resolution.width == 0)
		{
			resolution = Screen.currentResolution;
			SaveOptions();
		}
		else
		{
			ApplyOptions();
		}
	}

	private void SaveOptions()
	{
		PlayerPrefs.SetFloat("Vol_Master", vol_Master);
		PlayerPrefs.SetFloat("vol_Sfx", vol_Sfx);
		PlayerPrefs.SetFloat("vol_Music", vol_Music);
		PlayerPrefs.SetInt("res_X", resolution.width);
		PlayerPrefs.SetInt("res_Y", resolution.height);
		PlayerPrefs.SetInt("fullScreen", (int)fullScreen);
		PlayerPrefs.SetInt("characterPattrens", GetInt(characterPattrens));
		PlayerPrefs.SetInt("mapPatterns", GetInt(mapPatterns));
		PlayerPrefs.SetInt("leftStickAim", GetInt(leftStickAim));
		PlayerPrefs.SetInt("vSync", GetInt(vSync));
		PlayerPrefs.SetInt("lockMouse", GetInt(lockMouse));
		PlayerPrefs.SetInt("lockStick", GetInt(lockStick));
		PlayerPrefs.SetInt("showCardStatNumbers", GetInt(showCardStatNumbers));
		ApplyOptions();
	}

	private void ApplyOptions()
	{
		Screen.SetResolution(resolution.width, resolution.height, (FullScreenMode)fullScreen);
		QualitySettings.vSyncCount = (vSync ? 1 : 0);
		SoundVolumeManager.Instance.SetAudioMixerVolumes(vol_Master, vol_Music, vol_Sfx);
	}

	private bool GetBool(int value)
	{
		if (value != 1)
		{
			return false;
		}
		return true;
	}

	private int GetInt(bool value)
	{
		if (!value)
		{
			return 0;
		}
		return 1;
	}

	public void SetResolution(Resolution resolutionToSet)
	{
		resolution = resolutionToSet;
		SaveOptions();
	}

	public void SetVSync(bool vSyncToSet)
	{
		vSync = vSyncToSet;
		SaveOptions();
	}

	public void SetVolMaster(float vol_Master_ToSet)
	{
		vol_Master = vol_Master_ToSet;
		SaveOptions();
	}

	public void SetVolSFX(float vol_SFX_ToSet)
	{
		vol_Sfx = vol_SFX_ToSet;
		SaveOptions();
	}

	public void SetVolMusic(float vol_Music_ToSet)
	{
		vol_Music = vol_Music_ToSet;
		SaveOptions();
	}

	public void SetCharacterPatterns(bool characterPatternsToSet)
	{
		characterPattrens = characterPatternsToSet;
		SaveOptions();
	}

	public void SetMapPatterns(bool mapPatternsToSet)
	{
		mapPatterns = mapPatternsToSet;
		SaveOptions();
	}

	public void SetLeftStickAim(bool leftStickAimToSet)
	{
		leftStickAim = leftStickAimToSet;
		SaveOptions();
	}

	public void lockMouseAim(bool setLockMouse)
	{
		lockMouse = setLockMouse;
		SaveOptions();
	}

	public void lockStickAim(bool setLockStick)
	{
		lockStick = setLockStick;
		SaveOptions();
	}

	public void SetShowCardStatNumbers(bool showCardStatNumbersToSet)
	{
		showCardStatNumbers = showCardStatNumbersToSet;
		SaveOptions();
	}

	public void SetFullScreen(FullScreenOption fullscreenToSet)
	{
		fullScreen = fullscreenToSet;
		SaveOptions();
	}
}
