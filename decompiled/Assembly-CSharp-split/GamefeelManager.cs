using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamefeelManager : MonoBehaviour
{
	public static GamefeelManager instance;

	private List<GameFeeler> m_GameFeelers = new List<GameFeeler>();

	private void Awake()
	{
		instance = this;
	}

	public static void RegisterGamefeeler(GameFeeler gameFeeler)
	{
		instance.m_GameFeelers.Add(gameFeeler);
	}

	public void AddUIGameFeel(Vector2 directionForce)
	{
		for (int i = 0; i < instance.m_GameFeelers.Count; i++)
		{
			instance.m_GameFeelers[i].OnUIGameFeel(directionForce);
		}
	}

	public void AddGameFeel(Vector2 directionForce)
	{
		for (int i = 0; i < instance.m_GameFeelers.Count; i++)
		{
			instance.m_GameFeelers[i].OnGameFeel(directionForce);
		}
	}

	public static void GameFeel(Vector2 directionForce)
	{
		for (int i = 0; i < instance.m_GameFeelers.Count; i++)
		{
			instance.m_GameFeelers[i].OnGameFeel(directionForce);
		}
	}

	public void AddUIGameFeelOverTime(float amount, float time)
	{
		StartCoroutine(DoUIGameFeelOverTime(amount, time));
	}

	private IEnumerator DoUIGameFeelOverTime(float amount, float time)
	{
		float startTime = time;
		while (time > 0f)
		{
			instance.AddUIGameFeel(amount * (Vector2)Random.onUnitSphere * Time.unscaledDeltaTime * (time / startTime));
			time -= Time.unscaledDeltaTime;
			yield return null;
		}
	}
}
