using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI.ProceduralImage;

public class TwitchAudienceVisualizer : MonoBehaviour
{
	private const int MAXIMUM_MSG_PER_PLAYER_IN_AUDIENCE = 100;

	private const int MAXIMUM_MSG_PER_SECOND = 10;

	private float m_RecieveRate;

	private float m_LastRecievedTime;

	public float countDownSpeed = 2f;

	public float totalAmountOfTime = 30f;

	[SerializeField]
	private GameObject m_TwitchChatObject;

	public ProceduralImage border;

	public TextMeshProUGUI audioenceText;

	private Screenshaker shake;

	private bool m_IsAudition;

	private bool m_IsReadyToSpawnChatObjects;

	private ListMenuPage m_Page;

	public static TwitchAudienceVisualizer instance;

	private Dictionary<string, int> m_MsgsPerTwitchUser = new Dictionary<string, int>();

	private int currentViewerScore;

	private void Awake()
	{
		instance = this;
		m_RecieveRate = 0.1f;
	}

	private void Start()
	{
		m_Page = GetComponentInParent<ListMenuPage>();
		shake = GetComponentInChildren<Screenshaker>();
		TwitchUIHandler.Instance.AddMsgAction(Chat);
		StartAudition();
	}

	public void Chat(string msg, string from)
	{
		if (!m_IsAudition)
		{
			return;
		}
		if (!m_MsgsPerTwitchUser.ContainsKey(from))
		{
			m_MsgsPerTwitchUser.Add(from, 0);
		}
		if (m_MsgsPerTwitchUser[from] >= 100)
		{
			return;
		}
		m_MsgsPerTwitchUser[from]++;
		if (Time.unscaledTime >= m_LastRecievedTime + m_RecieveRate)
		{
			m_LastRecievedTime = Time.unscaledTime;
			if (m_IsReadyToSpawnChatObjects)
			{
				SpawnChatObject(msg, from);
			}
		}
		currentViewerScore += 100;
		shake.OnUIGameFeel(Random.insideUnitCircle.normalized);
	}

	private void SpawnChatObject(string msg, string from)
	{
		Object.Instantiate(m_TwitchChatObject).GetComponentInChildren<TextMeshProUGUI>().text = msg;
	}

	private void StartAudition()
	{
		m_IsAudition = true;
		m_MsgsPerTwitchUser = new Dictionary<string, int>();
		StartCoroutine(DoAudition());
	}

	private void Shake(float m = 1f)
	{
		shake.OnUIGameFeel(Random.insideUnitCircle.normalized * m);
	}

	private IEnumerator DoAudition()
	{
		border.fillAmount = 0f;
		audioenceText.text = "";
		yield return new WaitForSecondsRealtime(1f / countDownSpeed);
		Shake(3f);
		audioenceText.text = "3";
		yield return new WaitForSecondsRealtime(1f / countDownSpeed);
		Shake(3f);
		audioenceText.text = "2";
		yield return new WaitForSecondsRealtime(1f / countDownSpeed);
		Shake(3f);
		audioenceText.text = "1";
		yield return new WaitForSecondsRealtime(1f / countDownSpeed);
		Shake(10f);
		audioenceText.text = "CHAT";
		yield return new WaitForSecondsRealtime(2f / countDownSpeed);
		Shake(10f);
		audioenceText.text = "MAKE";
		yield return new WaitForSecondsRealtime(1f / countDownSpeed);
		Shake(10f);
		audioenceText.text = "SOME";
		yield return new WaitForSecondsRealtime(1f / countDownSpeed);
		Shake(10f);
		audioenceText.text = "NOISE";
		float t = 0f;
		while (t < 1f)
		{
			t += Time.unscaledDeltaTime;
			Shake(3f - t);
			yield return null;
		}
		yield return new WaitForSecondsRealtime(0.5f / countDownSpeed);
		m_IsReadyToSpawnChatObjects = true;
		float c = totalAmountOfTime;
		while (c > 0f)
		{
			border.fillAmount = c / totalAmountOfTime;
			c -= Time.unscaledDeltaTime;
			audioenceText.text = currentViewerScore + "\n<size=50>AUDIENCE RATING</size>";
			yield return null;
		}
		m_IsAudition = false;
		m_IsReadyToSpawnChatObjects = false;
		m_Page.Close();
		NetworkConnectionHandler.instance.TwitchJoin(currentViewerScore);
	}
}
