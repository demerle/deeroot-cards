using UnityEngine;

public class ObjectShake : MonoBehaviour
{
	public float globalMultiplier = 1f;

	public float interval = 0.1f;

	public float movementMultiplier = 1f;

	public float rotationMultiplier = 1f;

	private float counter;

	private void Start()
	{
	}

	private void Update()
	{
		counter += TimeHandler.deltaTime;
		if (counter > interval)
		{
			base.transform.localPosition = Random.insideUnitCircle * movementMultiplier * globalMultiplier * 0.1f;
			base.transform.localEulerAngles = new Vector3(base.transform.localEulerAngles.x, base.transform.localEulerAngles.y, (float)Random.Range(-10, 10) * globalMultiplier * rotationMultiplier);
			counter = 0f;
		}
	}
}
