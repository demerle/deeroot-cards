using System.Collections;
using SoundImplementation;
using TMPro;
using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
	private const string SEARCHING_TEXT = "SEARCHING";

	private const string SEARCHING_PRIVATE_TEXT = "WAITING FOR FRIEND";

	[SerializeField]
	private TextMeshProUGUI m_SearchingText;

	public GameObject gameMode;

	public GeneralParticleSystem searchingSystem;

	public GeneralParticleSystem matchFoundSystem;

	public GeneralParticleSystem[] playerNamesSystem;

	public float matchFoundTime = 0.5f;

	public float playerNameTime = 2f;

	public static LoadingScreen instance;

	private void Awake()
	{
		instance = this;
	}

	public void StartLoading(bool privateGame = false)
	{
		StopAllCoroutines();
		matchFoundSystem.Stop();
		for (int i = 0; i < playerNamesSystem.Length; i++)
		{
			playerNamesSystem[i].Stop();
		}
		searchingSystem.Play();
		m_SearchingText.text = GetSearchingString(privateGame);
	}

	private string GetSearchingString(bool privGame)
	{
		if (privGame)
		{
			return "WAITING FOR FRIEND";
		}
		return "SEARCHING";
	}

	private IEnumerator IDoLoading()
	{
		SoundPlayerStatic.Instance.PlayMatchFound();
		matchFoundSystem.Play();
		yield return new WaitForSeconds(matchFoundTime);
		matchFoundSystem.Stop();
		GetComponentInChildren<DisplayMatchPlayerNames>().ShowNames();
		for (int i = 0; i < playerNamesSystem.Length; i++)
		{
			playerNamesSystem[i].Play();
		}
		yield return new WaitForSeconds(playerNameTime);
		for (int j = 0; j < playerNamesSystem.Length; j++)
		{
			playerNamesSystem[j].Stop();
			playerNamesSystem[j].GetComponentInParent<TextMeshProUGUI>().text = "";
		}
		gameMode.SetActive(true);
	}

	public void StopLoading()
	{
		StopAllCoroutines();
		searchingSystem.Stop();
		StartCoroutine(IDoLoading());
	}
}
