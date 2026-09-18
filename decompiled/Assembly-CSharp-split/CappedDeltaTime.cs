using UnityEngine;

public class CappedDeltaTime : MonoBehaviour
{
	public static float time;

	private void Update()
	{
		time = Mathf.Clamp(TimeHandler.deltaTime, 0f, 0.02f);
	}
}
