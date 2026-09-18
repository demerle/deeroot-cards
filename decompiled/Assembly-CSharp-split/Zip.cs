using UnityEngine;

public class Zip : MonoBehaviour
{
	public float multiplier = 1f;

	public float turn = 0.2f;

	private float count;

	private int up = 1;

	private void Start()
	{
	}

	private void Update()
	{
		count += TimeHandler.deltaTime;
		if (count > turn)
		{
			count = 0f;
			up *= -1;
		}
		base.transform.root.position += base.transform.up * multiplier * up * Time.smoothDeltaTime;
	}
}
