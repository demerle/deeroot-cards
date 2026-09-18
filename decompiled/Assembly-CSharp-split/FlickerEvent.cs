using UnityEngine;
using UnityEngine.Events;

public class FlickerEvent : MonoBehaviour
{
	public UnityEvent onEvent;

	public UnityEvent offEvent;

	public float interval;

	private float c;

	public bool isOn;

	private bool flickedOn;

	private void Start()
	{
	}

	private void Update()
	{
		c += TimeHandler.deltaTime;
		if (isOn)
		{
			if (c > interval)
			{
				flickedOn = !flickedOn;
				if (flickedOn)
				{
					onEvent.Invoke();
				}
				else
				{
					offEvent.Invoke();
				}
				c = 0f;
			}
		}
		else if (flickedOn && c > interval)
		{
			c = 0f;
			offEvent.Invoke();
			flickedOn = false;
		}
	}
}
