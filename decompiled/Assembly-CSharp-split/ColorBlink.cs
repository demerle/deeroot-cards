using System.Collections;
using UnityEngine;

public class ColorBlink : MonoBehaviour
{
	public Color blinkColor;

	public float timeAmount;

	private Color defaultColor;

	private SpriteRenderer sprite;

	private bool inited;

	private void Start()
	{
		if (!inited)
		{
			inited = true;
			sprite = GetComponent<SpriteRenderer>();
			defaultColor = sprite.color;
		}
	}

	public void DoBlink()
	{
		StopAllCoroutines();
		StartCoroutine(IDoBlink());
	}

	private IEnumerator IDoBlink()
	{
		if (!sprite)
		{
			Start();
		}
		sprite.color = blinkColor;
		yield return new WaitForSeconds(timeAmount);
		sprite.color = defaultColor;
	}
}
