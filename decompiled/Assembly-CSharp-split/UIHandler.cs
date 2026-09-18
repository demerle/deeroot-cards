using System;
using System.Collections;
using Sonigon;
using TMPro;
using UnityEngine;

public class UIHandler : MonoBehaviour
{
	[Header("Sounds")]
	public SoundEvent soundTextAppear;

	public SoundEvent soundTextDisappear;

	public static UIHandler instance;

	[Header("Settings")]
	public TextMeshProUGUI gameOverText;

	public GeneralParticleSystem gameOverTextPart;

	public GeneralParticleSystem roundBackgroundPart;

	public GeneralParticleSystem roundTextPart;

	public CodeAnimation roundCounterAnim;

	public RoundCounter roundCounter;

	public RoundCounter roundCounterSmall;

	public CodeAnimation roundCounterAnimSmall;

	public GameObject pickerObject;

	public GeneralParticleSystem pickerPart;

	public GeneralParticleSystem joinGamePart;

	public TextMeshProUGUI jointGameText;

	public PopUpHandler popUpHandler;

	private void Awake()
	{
		instance = this;
		popUpHandler = base.transform.root.GetComponentInChildren<PopUpHandler>();
	}

	public void ShowJoinGameText(string text, Color color)
	{
		SoundManager.Instance.Play(soundTextAppear, base.transform);
		if (text != "")
		{
			jointGameText.text = text;
		}
		if (color != Color.black)
		{
			joinGamePart.particleSettings.color = color;
		}
		joinGamePart.loop = true;
		joinGamePart.Play();
	}

	internal void SetNumberOfRounds(int roundsToWinGame)
	{
		roundCounter.SetNumberOfRounds(roundsToWinGame);
		roundCounterSmall.SetNumberOfRounds(roundsToWinGame);
	}

	public void HideJoinGameText()
	{
		SoundManager.Instance.Play(soundTextDisappear, base.transform);
		joinGamePart.Stop();
	}

	public void DisplayScreenText(Color color, string text, float speed)
	{
		gameOverTextPart.particleSettings.color = color;
		gameOverTextPart.duration = 60f / speed;
		gameOverTextPart.Play();
		gameOverText.text = text;
	}

	public void DisplayScreenTextLoop(string text)
	{
		gameOverTextPart.duration = 60f;
		gameOverTextPart.loop = true;
		gameOverTextPart.Play();
		gameOverText.text = text;
	}

	public void DisplayScreenTextLoop(Color color, string text)
	{
		gameOverTextPart.particleSettings.color = color;
		gameOverTextPart.duration = 60f;
		gameOverTextPart.loop = true;
		gameOverTextPart.Play();
		gameOverText.text = text;
	}

	public void StopScreenTextLoop()
	{
		gameOverTextPart.loop = false;
	}

	public void ShowAddPoint(Color color, string winTextBefore, string text, float speed)
	{
		gameOverTextPart.particleSettings.color = color;
		gameOverTextPart.duration = 60f / speed;
		gameOverTextPart.Play();
		StartCoroutine(DoShowAddPoint(winTextBefore, text));
	}

	private IEnumerator DoShowAddPoint(string winTextBefore, string text)
	{
		gameOverText.text = winTextBefore;
		yield return new WaitForSecondsRealtime(0.7f);
		gameOverText.text = text;
	}

	public void ShowRoundOver(int p1Rounds, int p2Rounds)
	{
		roundCounter.gameObject.SetActive(value: true);
		roundBackgroundPart.Play();
		roundTextPart.Play();
		roundCounterAnim.PlayIn();
		roundCounter.UpdateRounds(p1Rounds, p2Rounds);
	}

	public void ShowRoundCounterSmall(int p1Rounds, int p2Rounds, int p1Points, int p2Points)
	{
		roundCounterSmall.gameObject.SetActive(value: true);
		roundCounterSmall.UpdateRounds(p1Rounds, p2Rounds);
		roundCounterSmall.UpdatePoints(p1Points, p2Points);
		if (roundCounterAnimSmall.currentState != CodeAnimationInstance.AnimationUse.In)
		{
			roundCounterAnimSmall.PlayIn();
		}
	}

	public void HideRoundCounterSmall()
	{
		roundCounterAnimSmall.PlayOut();
	}

	public void ShowPicker(int pickerID, PickerType pickerType = PickerType.Team)
	{
		pickerObject.SetActive(value: true);
		if (pickerType == PickerType.Team)
		{
			pickerPart.particleSettings.color = PlayerManager.instance.GetColorFromTeam(pickerID).winText;
		}
		if (pickerType == PickerType.Player)
		{
			pickerPart.particleSettings.color = PlayerManager.instance.GetColorFromPlayer(pickerID).winText;
		}
		pickerPart.loop = true;
		pickerPart.Play();
	}

	public void StopShowPicker()
	{
		StartCoroutine(DoStopShowPicker());
	}

	private IEnumerator DoStopShowPicker()
	{
		pickerPart.loop = false;
		yield return new WaitForSeconds(0.3f);
		pickerObject.SetActive(value: false);
	}

	internal void DisplayYesNoLoop(Player pickingPlayer, Action<PopUpHandler.YesNo> functionToCall)
	{
		popUpHandler.StartPicking(pickingPlayer, functionToCall);
	}
}
