using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.ProceduralImage;

public class CounterUI : MonoBehaviour
{
	[Range(0f, 1f)]
	public float counter;

	public float timeToFill = 10f;

	public ProceduralImage outerRing;

	public ProceduralImage fill;

	public Transform rotator;

	public Transform still;

	private float remainingDuration;

	private bool isAbyssalForm;

	private bool done;

	public UnityEvent doneEvent;

	private void ResetStuff()
	{
		counter = 0f;
	}

	private void Update()
	{
		if (!done)
		{
			outerRing.fillAmount = counter;
			fill.fillAmount = counter;
			rotator.transform.localEulerAngles = new Vector3(0f, 0f, 0f - Mathf.Lerp(0f, 360f, counter));
			counter += TimeHandler.deltaTime / timeToFill;
			counter = Mathf.Clamp(counter, -0.1f / timeToFill, 1f);
			if (counter >= 1f)
			{
				done = true;
				doneEvent.Invoke();
			}
			if (counter <= 0f)
			{
				rotator.gameObject.SetActive(value: false);
				still.gameObject.SetActive(value: false);
			}
			else
			{
				rotator.gameObject.SetActive(value: true);
				still.gameObject.SetActive(value: true);
			}
		}
	}
}
