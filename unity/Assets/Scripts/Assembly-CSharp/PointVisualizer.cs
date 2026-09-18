using System.Collections;
using Sirenix.OdinInspector;
using Sonigon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PointVisualizer : MonoBehaviour
{
	public SoundEvent soundWinRound;

	public SoundEvent sound_UI_Arms_Race_A_Ball_Shrink_Go_To_Left_Corner;

	public SoundEvent sound_UI_Arms_Race_B_Ball_Go_Down_Then_Expand;

	public SoundEvent sound_UI_Arms_Race_C_Ball_Pop_Shake;

	public static PointVisualizer instance;

	public AnimationCurve moveCurve;

	public AnimationCurve scaleCurve;

	public float timeBetween;

	public float timeToMove = 0.2f;

	public float timeToScale = 0.2f;

	public TextMeshProUGUI text;

	public GameObject bg;

	public Transform orangeBall;

	public Transform blueBall;

	private RectTransform orangeBallRT;

	private RectTransform blueBallRT;

	public Image orangeFill;

	public Image blueFill;

	private Vector3 orangeVel;

	private Vector3 blueVel;

	private Vector3 orangeSP;

	private Vector3 blueSP;

	private float ballBaseSize = 200f;

	private float ballSmallSize = 20f;

	private float bigBallScale = 900f;

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		orangeBallRT = orangeBall.GetComponent<RectTransform>();
		blueBallRT = blueBall.GetComponent<RectTransform>();
		orangeSP = orangeBall.GetComponent<RectTransform>().anchoredPosition;
		blueSP = blueBall.GetComponent<RectTransform>().anchoredPosition;
		Close();
	}

	[Button]
	private void TestWinSequence()
	{
		StartCoroutine(DoWinSequence(2, 1, 2, 1, true));
	}

	[Button]
	private void TestPoint()
	{
		StartCoroutine(DoSequence(1, 0, true));
	}

	[Button]
	private void Reset()
	{
		StartCoroutine(DoSequence(0, 0, true));
	}

	public void ResetPoints()
	{
		orangeFill.fillAmount = 0f;
		blueFill.fillAmount = 0f;
	}

	private void ResetBalls()
	{
		orangeBallRT.sizeDelta = Vector2.one * ballBaseSize;
		blueBallRT.sizeDelta = Vector2.one * ballBaseSize;
		orangeBall.GetComponent<RectTransform>().anchoredPosition = orangeSP;
		blueBall.GetComponent<RectTransform>().anchoredPosition = blueSP;
		orangeVel = Vector3.zero;
		blueVel = Vector3.zero;
	}

	public IEnumerator DoWinSequence(int orangePoints, int bluePoints, int orangeRounds, int blueRounds, bool orangeWinner)
	{
		yield return new WaitForSecondsRealtime(0.35f);
		SoundManager.Instance.Play(soundWinRound, base.transform);
		ResetBalls();
		bg.SetActive(true);
		blueBall.gameObject.SetActive(true);
		orangeBall.gameObject.SetActive(true);
		yield return new WaitForSecondsRealtime(0.2f);
		GamefeelManager.instance.AddUIGameFeelOverTime(10f, 0.1f);
		DoShowPoints(orangePoints, bluePoints, orangeWinner);
		yield return new WaitForSecondsRealtime(0.35f);
		SoundManager.Instance.Play(sound_UI_Arms_Race_A_Ball_Shrink_Go_To_Left_Corner, base.transform);
		float c3 = 0f;
		while (c3 < timeToScale)
		{
			if (orangeWinner)
			{
				orangeBallRT.sizeDelta = Vector2.LerpUnclamped(orangeBallRT.sizeDelta, Vector2.one * ballSmallSize, scaleCurve.Evaluate(c3 / timeToScale));
			}
			else
			{
				blueBallRT.sizeDelta = Vector2.LerpUnclamped(blueBallRT.sizeDelta, Vector2.one * ballSmallSize, scaleCurve.Evaluate(c3 / timeToScale));
			}
			c3 += Time.unscaledDeltaTime;
			yield return null;
		}
		yield return new WaitForSecondsRealtime(timeBetween);
		c3 = 0f;
		while (c3 < timeToMove)
		{
			if (orangeWinner)
			{
				orangeBall.position = Vector3.LerpUnclamped(orangeBall.position, UIHandler.instance.roundCounterSmall.GetPointPos(0), scaleCurve.Evaluate(c3 / timeToMove));
			}
			else
			{
				blueBall.position = Vector3.LerpUnclamped(blueBall.position, UIHandler.instance.roundCounterSmall.GetPointPos(1), scaleCurve.Evaluate(c3 / timeToMove));
			}
			c3 += Time.unscaledDeltaTime;
			yield return null;
		}
		SoundManager.Instance.Play(sound_UI_Arms_Race_B_Ball_Go_Down_Then_Expand, base.transform);
		if (orangeWinner)
		{
			orangeBall.position = UIHandler.instance.roundCounterSmall.GetPointPos(0);
		}
		else
		{
			blueBall.position = UIHandler.instance.roundCounterSmall.GetPointPos(1);
		}
		yield return new WaitForSecondsRealtime(timeBetween);
		c3 = 0f;
		while (c3 < timeToMove)
		{
			if (!orangeWinner)
			{
				orangeBall.position = Vector3.LerpUnclamped(orangeBall.position, CardChoiceVisuals.instance.transform.position, scaleCurve.Evaluate(c3 / timeToMove));
			}
			else
			{
				blueBall.position = Vector3.LerpUnclamped(blueBall.position, CardChoiceVisuals.instance.transform.position, scaleCurve.Evaluate(c3 / timeToMove));
			}
			c3 += Time.unscaledDeltaTime;
			yield return null;
		}
		if (!orangeWinner)
		{
			orangeBall.position = CardChoiceVisuals.instance.transform.position;
		}
		else
		{
			blueBall.position = CardChoiceVisuals.instance.transform.position;
		}
		yield return new WaitForSecondsRealtime(timeBetween);
		c3 = 0f;
		while (c3 < timeToScale)
		{
			if (!orangeWinner)
			{
				orangeBallRT.sizeDelta = Vector2.LerpUnclamped(orangeBallRT.sizeDelta, Vector2.one * bigBallScale, scaleCurve.Evaluate(c3 / timeToScale));
			}
			else
			{
				blueBallRT.sizeDelta = Vector2.LerpUnclamped(blueBallRT.sizeDelta, Vector2.one * bigBallScale, scaleCurve.Evaluate(c3 / timeToScale));
			}
			c3 += Time.unscaledDeltaTime;
			yield return null;
		}
		SoundManager.Instance.Play(sound_UI_Arms_Race_C_Ball_Pop_Shake, base.transform);
		GamefeelManager.instance.AddUIGameFeelOverTime(10f, 0.2f);
		CardChoiceVisuals.instance.Show(orangeWinner ? 1 : 0);
		UIHandler.instance.roundCounterSmall.UpdateRounds(orangeRounds, blueRounds);
		UIHandler.instance.roundCounterSmall.UpdatePoints(0, 0);
		DoShowPoints(0, 0, orangeWinner);
		Close();
	}

	private void MoveTowards()
	{
	}

	public IEnumerator DoSequence(int currentOrange, int currentBlue, bool orangeWinner)
	{
		yield return new WaitForSecondsRealtime(0.45f);
		SoundManager.Instance.Play(soundWinRound, base.transform);
		ResetBalls();
		bg.SetActive(true);
		blueBall.gameObject.SetActive(true);
		orangeBall.gameObject.SetActive(true);
		yield return new WaitForSecondsRealtime(0.2f);
		GamefeelManager.instance.AddUIGameFeelOverTime(10f, 0.1f);
		DoShowPoints(currentOrange, currentBlue, orangeWinner);
		yield return new WaitForSecondsRealtime(1.8f);
		orangeBall.GetComponent<CurveAnimation>().PlayOut();
		blueBall.GetComponent<CurveAnimation>().PlayOut();
		yield return new WaitForSecondsRealtime(0.25f);
		Close();
	}

	public void DoShowPoints(int currentOrange, int currentBlue, bool orangeWinner)
	{
		orangeFill.fillAmount = (float)currentOrange * 0.5f;
		blueFill.fillAmount = (float)currentBlue * 0.5f;
		if (orangeWinner)
		{
			text.color = PlayerSkinBank.GetPlayerSkinColors(0).winText;
		}
		else
		{
			text.color = PlayerSkinBank.GetPlayerSkinColors(1).winText;
		}
		if (orangeWinner)
		{
			if (currentOrange > 1)
			{
				RoundOrange();
			}
			else
			{
				HalfOrange();
			}
		}
		else if (currentBlue > 1)
		{
			RoundOBlue();
		}
		else
		{
			HalfBlue();
		}
	}

	private void Close()
	{
		text.text = "";
		bg.SetActive(false);
		blueBall.gameObject.SetActive(false);
		orangeBall.gameObject.SetActive(false);
	}

	private void HalfOrange()
	{
		text.text = "HALF ORANGE";
	}

	private void RoundOrange()
	{
		text.text = "ROUND ORANGE";
	}

	private void HalfBlue()
	{
		text.text = "HALF BLUE";
	}

	private void RoundOBlue()
	{
		text.text = "ROUND BLUE";
	}

	private void Update()
	{
	}
}
