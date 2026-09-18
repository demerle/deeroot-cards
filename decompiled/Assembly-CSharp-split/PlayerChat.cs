using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerChat : MonoBehaviour
{
	public float spring = 15f;

	public float damper = 15f;

	public float impulse;

	public float targetScale;

	private TextMeshProUGUI messageText;

	private ScaleShake scaleShake;

	private float currentScale;

	private float vel;

	public Transform target;

	private Screenshaker shaker;

	private float sinceType;

	public float shakeAmount;

	public float shakeSpeed = 1f;

	private void Start()
	{
		messageText = target.GetComponentInChildren<TextMeshProUGUI>();
		shaker = target.GetComponentInChildren<Screenshaker>();
		target.transform.localScale = Vector3.zero;
	}

	private void Update()
	{
		sinceType -= Time.unscaledDeltaTime;
		if (sinceType < 0f)
		{
			targetScale = 0f;
		}
		else
		{
			targetScale = 1f;
		}
		vel = FRILerp.Lerp(vel, (targetScale - currentScale) * spring, damper);
		currentScale += Time.unscaledDeltaTime * vel;
		if (currentScale < 0f)
		{
			vel = 0f;
			currentScale = 0f;
		}
		target.transform.localScale = Vector3.one * currentScale;
	}

	public void Send(string message)
	{
		message = ChatFilter.instance.FilterMessage(message);
		messageText.text = message;
		if (sinceType > 0f)
		{
			vel += impulse;
		}
		sinceType = 2f + (float)message.Length * 0.05f;
		targetScale = 1f;
		if (message.ToUpper() == message)
		{
			StartCoroutine(ShakeOverTime(sinceType));
		}
	}

	private IEnumerator ShakeOverTime(float t)
	{
		float a = t;
		while (a > 0f)
		{
			shaker.OnUIGameFeel(shakeAmount * Random.insideUnitCircle);
			a -= Time.unscaledDeltaTime * shakeSpeed;
			yield return null;
		}
	}
}
