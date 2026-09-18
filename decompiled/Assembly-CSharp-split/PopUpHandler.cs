using System;
using UnityEngine;

public class PopUpHandler : MonoBehaviour
{
	public enum YesNo
	{
		Yes,
		No
	}

	public CurveAnimation yesAnim;

	public CurveAnimation noAnim;

	public bool isPicking;

	public GeneralParticleSystem yesPart;

	public GeneralParticleSystem noPart;

	public GeneralInput playerInput;

	private Action<YesNo> fToCall;

	private YesNo currentYesNo;

	private void Start()
	{
	}

	public void StartPicking(Player player, Action<YesNo> functionToCall)
	{
		PlayerManager.instance.RevivePlayers();
		PlayerManager.instance.SetPlayersSimulated(simulated: true);
		isPicking = true;
		fToCall = functionToCall;
		yesPart.particleSettings.color = PlayerManager.instance.GetColorFromTeam(player.teamID).winText;
		noPart.particleSettings.color = PlayerManager.instance.GetColorFromTeam(player.teamID).winText;
		yesPart.loop = true;
		yesPart.Play();
		yesAnim.PlayIn();
		noPart.loop = true;
		noPart.Play();
	}

	private void DonePicking()
	{
		fToCall(currentYesNo);
		isPicking = false;
		noPart.loop = false;
		yesPart.loop = false;
	}

	private void Update()
	{
		if (!isPicking)
		{
			return;
		}
		for (int i = 0; i < PlayerManager.instance.players.Count; i++)
		{
			if (PlayerManager.instance.players[i].data.view.IsMine)
			{
				playerInput = PlayerManager.instance.players[i].data.input;
				if (playerInput.stickPressDir == GeneralInput.StickDirection.Left && currentYesNo != YesNo.Yes)
				{
					currentYesNo = YesNo.Yes;
					UpdateUI();
				}
				if (playerInput.stickPressDir == GeneralInput.StickDirection.Right && currentYesNo != YesNo.No)
				{
					currentYesNo = YesNo.No;
					UpdateUI();
				}
				if (playerInput.acceptWasPressed)
				{
					DonePicking();
				}
			}
		}
	}

	private void UpdateUI()
	{
		if (currentYesNo == YesNo.Yes)
		{
			yesAnim.PlayIn();
			noAnim.PlayOut();
		}
		else
		{
			yesAnim.PlayOut();
			noAnim.PlayIn();
		}
	}
}
