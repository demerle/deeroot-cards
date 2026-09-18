using System.Collections;
using Photon.Pun;
using UnityEngine;

public class TimeHandler : MonoBehaviour
{
	public AnimationCurve slowDown;

	public AnimationCurve speedUp;

	public float baseTimeScale = 0.85f;

	public float gameOverTime = 1f;

	public float gameStartTime;

	public float timeStop = 1f;

	public static float timeScale = 1f;

	public static TimeHandler instance;

	public static float deltaTime;

	public static float fixedDeltaTime;

	private void Awake()
	{
		instance = this;
	}

	private void Update()
	{
		float num = baseTimeScale;
		if (gameOverTime < 1f)
		{
			num *= gameOverTime;
		}
		if (gameStartTime < 1f)
		{
			num *= gameStartTime;
		}
		if (timeStop < 1f)
		{
			num *= timeStop;
		}
		if (PhotonNetwork.OfflineMode && EscapeMenuHandler.isEscMenu)
		{
			num *= 0f;
		}
		timeScale = num;
		deltaTime = Time.deltaTime * timeScale;
		fixedDeltaTime = Time.fixedDeltaTime * timeScale;
		Time.timeScale = 1f;
	}

	public void StartGame()
	{
		gameStartTime = 1f;
	}

	public void DoSpeedUp()
	{
		StartCoroutine(DoCurve(speedUp));
	}

	public void DoSlowDown()
	{
		StartCoroutine(DoCurve(slowDown));
	}

	private IEnumerator DoCurve(AnimationCurve curve)
	{
		float c = 0f;
		float t = curve.keys[curve.keys.Length - 1].time;
		while (c < t)
		{
			gameOverTime = curve.Evaluate(c);
			c += Time.unscaledDeltaTime;
			yield return null;
		}
		gameOverTime = curve.Evaluate(t);
	}

	public void HitStop()
	{
		StartCoroutine(DoHitStop());
	}

	private IEnumerator DoHitStop()
	{
		timeStop = 0f;
		yield return new WaitForSeconds(0.3f);
		timeStop = 1f;
	}
}
