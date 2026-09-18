using UnityEngine;
using UnityEngine.Analytics;

public class GameAnalytics : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.P))
		{
			Analytics.SendEvent("PlayerKills", 5);
		}
	}
}
