using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	[Header("Settings")]
	public static bool lockInput;

	public static GameManager instance;

	public bool isPlaying;

	public bool battleOngoing;

	public Action<int, int> GameOverAction;

	public void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		lockInput = false;
	}

	private void Update()
	{
	}

	public void GameOver(int winingTeamID, int killedTeamID)
	{
		if (GameOverAction != null)
		{
			GameOverAction(winingTeamID, killedTeamID);
		}
	}
}
