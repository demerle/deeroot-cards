using System.Collections;
using TMPro;
using UnityEngine;

public class MenuEffects : MonoBehaviour
{
	public static MenuEffects instance;

	public Color nopeColor;

	private void Awake()
	{
		instance = this;
	}

	public void ShakeObject(GameObject objectToShake, Vector3 defaultPos, float amount, float time)
	{
		StartCoroutine(DoShakeObject(objectToShake, defaultPos, amount, time));
	}

	private IEnumerator DoShakeObject(GameObject objectToShake, Vector3 defaultPos, float amount, float time)
	{
		float c = 0f;
		while (c < time)
		{
			Vector3 localPosition = defaultPos + Random.onUnitSphere * amount * ((time - c) / time);
			localPosition.z = defaultPos.z;
			objectToShake.transform.localPosition = localPosition;
			c += Time.unscaledDeltaTime;
			yield return null;
		}
		objectToShake.transform.localPosition = defaultPos;
	}

	public void BlinkInColor(TextMeshProUGUI textToBlink, Color blinkColor, Color defaultColor, float seconds)
	{
		StartCoroutine(DoTextColorBlink(textToBlink, blinkColor, defaultColor, seconds));
	}

	private IEnumerator DoTextColorBlink(TextMeshProUGUI textToBlink, Color blinkColor, Color defaultColor, float seconds)
	{
		float c = 0f;
		while (c < seconds)
		{
			textToBlink.color = blinkColor;
			c += Time.unscaledDeltaTime;
			yield return null;
		}
		textToBlink.color = defaultColor;
	}
}
