using UnityEngine;

public class Cos : MonoBehaviour
{
	public float multiplier = 1f;

	private void Update()
	{
		base.transform.root.position += base.transform.right * Mathf.Cos(Time.time * 20f * multiplier) * 10f * multiplier * Time.smoothDeltaTime;
	}
}
