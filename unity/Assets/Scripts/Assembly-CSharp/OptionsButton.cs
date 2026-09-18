using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionsButton : MonoBehaviour
{
	public enum SettingsTarget
	{
		Resolution = 0,
		Vol_Master = 1,
		Vol_SFX = 2,
		Vol_Music = 3,
		CharacterPattern = 4,
		MapPattern = 5,
		leftStickAim = 6,
		lockAimDirections = 7,
		ShowCardStarts = 8,
		FullScreen = 9,
		FreeStickAim = 10,
		Vsync = 11
	}

	public enum SettingsType
	{
		Binary = 0,
		Slider = 1,
		MultiOption = 2
	}

	public SettingsTarget settingsTarget;

	public SettingsType settingsType;

	public bool currentBoolValue;

	public Resolution currentResolutionValue;

	public Optionshandler.FullScreenOption currentFullscreenValue;

	public float currentFloatValue;

	private TextMeshProUGUI text;

	public Slider slider;

	private void Awake()
	{
		text = GetComponentsInChildren<TextMeshProUGUI>(true)[1];
		if (settingsType == SettingsType.Slider)
		{
			slider = GetComponentInChildren<Slider>(true);
			slider.onValueChanged.AddListener(SliderSlide);
		}
		else
		{
			GetComponent<Button>().onClick.AddListener(ClickButton);
		}
	}

	private void OnEnable()
	{
		switch (settingsTarget)
		{
		case SettingsTarget.Resolution:
			currentResolutionValue = Optionshandler.resolution;
			break;
		case SettingsTarget.Vol_Master:
			currentFloatValue = Optionshandler.vol_Master;
			break;
		case SettingsTarget.Vol_SFX:
			currentFloatValue = Optionshandler.vol_Sfx;
			break;
		case SettingsTarget.Vol_Music:
			currentFloatValue = Optionshandler.vol_Music;
			break;
		case SettingsTarget.CharacterPattern:
			currentBoolValue = Optionshandler.characterPattrens;
			break;
		case SettingsTarget.MapPattern:
			currentBoolValue = Optionshandler.mapPatterns;
			break;
		case SettingsTarget.leftStickAim:
			currentBoolValue = Optionshandler.leftStickAim;
			break;
		case SettingsTarget.lockAimDirections:
			currentBoolValue = Optionshandler.lockMouse;
			break;
		case SettingsTarget.FreeStickAim:
			currentBoolValue = Optionshandler.lockStick;
			break;
		case SettingsTarget.ShowCardStarts:
			currentBoolValue = Optionshandler.showCardStatNumbers;
			break;
		case SettingsTarget.FullScreen:
			currentFullscreenValue = Optionshandler.fullScreen;
			break;
		case SettingsTarget.Vsync:
			currentBoolValue = Optionshandler.vSync;
			break;
		}
		if (settingsType == SettingsType.Slider)
		{
			slider.value = currentFloatValue;
		}
		ReDraw();
	}

	public void SetResolutionAndFullscreen(Resolution newRess, Optionshandler.FullScreenOption fullScreen)
	{
		currentResolutionValue = newRess;
		currentFullscreenValue = fullScreen;
		ClickButton();
		if (settingsTarget == SettingsTarget.Resolution)
		{
			Optionshandler.instance.SetResolution(newRess);
		}
		if (settingsTarget == SettingsTarget.FullScreen)
		{
			Optionshandler.instance.SetFullScreen(fullScreen);
		}
	}

	public void SliderChanged()
	{
		currentFloatValue = slider.value;
	}

	private void SliderSlide(float newVal)
	{
		currentFloatValue = newVal;
		ClickButton();
	}

	public void ClickButton()
	{
		if (settingsType == SettingsType.Binary)
		{
			currentBoolValue = !currentBoolValue;
		}
		switch (settingsTarget)
		{
		case SettingsTarget.Resolution:
			base.transform.parent.parent.GetComponentInChildren<MultiOptions>(true).Open(settingsTarget, base.transform.GetChild(1).position, this);
			break;
		case SettingsTarget.Vol_Master:
			Optionshandler.instance.SetVolMaster(currentFloatValue);
			break;
		case SettingsTarget.Vol_SFX:
			Optionshandler.instance.SetVolSFX(currentFloatValue);
			break;
		case SettingsTarget.Vol_Music:
			Optionshandler.instance.SetVolMusic(currentFloatValue);
			break;
		case SettingsTarget.CharacterPattern:
			Optionshandler.instance.SetCharacterPatterns(currentBoolValue);
			break;
		case SettingsTarget.MapPattern:
			Optionshandler.instance.SetMapPatterns(currentBoolValue);
			break;
		case SettingsTarget.leftStickAim:
			Optionshandler.instance.SetLeftStickAim(currentBoolValue);
			break;
		case SettingsTarget.lockAimDirections:
			Optionshandler.instance.lockMouseAim(currentBoolValue);
			break;
		case SettingsTarget.FreeStickAim:
			Optionshandler.instance.lockStickAim(currentBoolValue);
			break;
		case SettingsTarget.ShowCardStarts:
			Optionshandler.instance.SetShowCardStatNumbers(currentBoolValue);
			break;
		case SettingsTarget.FullScreen:
			base.transform.parent.parent.GetComponentInChildren<MultiOptions>(true).Open(settingsTarget, base.transform.GetChild(1).position, this);
			break;
		case SettingsTarget.Vsync:
			Optionshandler.instance.SetVSync(currentBoolValue);
			break;
		}
		ReDraw();
	}

	private void ReDraw()
	{
		if (settingsType == SettingsType.Binary)
		{
			text.text = (currentBoolValue ? "YES" : "NO");
		}
		if (settingsType == SettingsType.MultiOption)
		{
			if (settingsTarget == SettingsTarget.Resolution)
			{
				text.text = currentResolutionValue.width + " X " + currentResolutionValue.height;
			}
			else if (currentFullscreenValue == Optionshandler.FullScreenOption.WindowedFullScreen)
			{
				text.text = "WINDOWED FULLSCREEN";
			}
			else
			{
				text.text = currentFullscreenValue.ToString().ToUpper();
			}
		}
	}
}
