using UnityEngine;

public class PositionNoise : CardAnimation
{
	public float amount;

	public float speed = 1f;

	private float startSeed;

	private Vector3 startPos;

	private void Start()
	{
		startPos = base.transform.localPosition;
		startSeed = Random.Range(0f, 100000f);
	}

	private void Update()
	{
		Vector2 vector = new Vector2(Mathf.PerlinNoise(startSeed + Time.unscaledTime * speed, startSeed + Time.unscaledTime * speed - 0.5f), Mathf.PerlinNoise(startSeed + Time.unscaledTime * speed, startSeed + Time.unscaledTime * speed) - 0.5f);
		base.transform.localPosition = startPos + (Vector3)vector * amount;
	}
}
