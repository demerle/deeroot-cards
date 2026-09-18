using System.Collections;
using UnityEngine;

public class BackgroundHandler : MonoBehaviour
{
	private Background[] backgrounds;

	private float untilSwitch;

	private void Start()
	{
		backgrounds = GetComponentsInChildren<Background>(includeInactive: true);
	}

	private void Update()
	{
		untilSwitch -= TimeHandler.deltaTime;
		if (untilSwitch < 0f)
		{
			SwitchBackground();
		}
	}

	private void SwitchBackground()
	{
		untilSwitch = Random.Range(30, 60);
		for (int i = 0; i < backgrounds.Length; i++)
		{
			backgrounds[i].ToggleBackground(on: false);
		}
		StartCoroutine(StartBackGroundSoon());
	}

	private IEnumerator StartBackGroundSoon()
	{
		yield return new WaitForSeconds(5f);
		backgrounds[Random.Range(0, backgrounds.Length)].ToggleBackground(on: true);
	}
}
