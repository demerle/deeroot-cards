using System.Collections;
using UnityEngine;

public class GridLightBulb : GridObject
{
	private SpriteRenderer rend;

	public Color maxLightColor;

	private Color baseColor;

	private bool isLitUp;

	private float currentDistance;

	private void Start()
	{
		rend = GetComponent<SpriteRenderer>();
		baseColor = rend.color;
	}

	public override void BopCall(float distance)
	{
		distance = 1f - distance;
		if (!isLitUp || (isLitUp && distance > currentDistance))
		{
			StopAllCoroutines();
			StartCoroutine(LightUp(distance));
		}
	}

	private IEnumerator LightUp(float distance)
	{
		isLitUp = true;
		currentDistance = distance;
		Color lightColor = Color.Lerp(maxLightColor, baseColor, distance);
		rend.color = lightColor;
		yield return new WaitForSeconds(Random.Range(0.5f, 2f));
		int blinks = Random.Range(0, 8);
		bool isOn = true;
		for (int i = 0; i < blinks; i++)
		{
			if (isOn)
			{
				rend.color = baseColor;
			}
			else
			{
				rend.color = lightColor;
			}
			isOn = !isOn;
			yield return new WaitForSeconds(Random.Range(0.05f, 0.1f));
		}
		rend.color = baseColor;
		isLitUp = false;
	}
}
