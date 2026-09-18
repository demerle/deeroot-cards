using UnityEngine;

public class CapScale : MonoBehaviour
{
	public float min = 2f;

	public float max = 10f;

	private void Start()
	{
		if (base.transform.localScale.x < min)
		{
			base.transform.localScale = Vector3.one * min;
		}
		if (base.transform.localScale.x > max)
		{
			base.transform.localScale = Vector3.one * max;
		}
	}
}
