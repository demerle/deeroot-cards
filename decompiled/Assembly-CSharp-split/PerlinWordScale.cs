using UnityEngine;

public class PerlinWordScale : MonoBehaviour
{
	public float scale = 1f;

	public float min = 0.5f;

	public float max = 2f;

	private void Start()
	{
		float t = Mathf.PerlinNoise(base.transform.position.x * scale, base.transform.position.y * scale);
		base.transform.localScale *= Mathf.Lerp(min, max, t);
	}
}
