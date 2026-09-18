using UnityEngine;

public class ScaleEvent : MonoBehaviour
{
	public ScaleEventInstace[] events;

	private void Start()
	{
		ScaleEventInstace scaleEventInstace = null;
		float num = 0f;
		for (int i = 0; i < events.Length; i++)
		{
			if (base.transform.localScale.x > events[i].threshold && events[i].threshold > num)
			{
				num = events[i].threshold;
				scaleEventInstace = events[i];
			}
		}
		scaleEventInstace?.scaleEvent.Invoke();
	}
}
