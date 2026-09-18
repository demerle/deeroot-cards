using UnityEngine;

public class Screenshaker : GameFeeler
{
	public bool ignoreTimeScale;

	public float spring = 100f;

	public float damper = 100f;

	public float shakeforce = 10f;

	public float threshold;

	private Vector2 velocity;

	private float clamp = 100f;

	private void Update()
	{
		float num = Mathf.Clamp(ignoreTimeScale ? Time.unscaledDeltaTime : TimeHandler.deltaTime, 0f, 0.015f);
		velocity -= velocity.normalized * Mathf.Pow(velocity.magnitude, 0.8f) * damper * num;
		velocity -= (Vector2)base.transform.localPosition * num * spring;
		base.transform.position += (Vector3)velocity * num;
	}

	private void ShakeInternal(Vector2 direction)
	{
		if (!(direction.magnitude < threshold))
		{
			direction = Vector2.ClampMagnitude(direction, clamp);
			velocity += direction * shakeforce;
		}
	}

	public override void OnGameFeel(Vector2 feelDirection)
	{
		if (!ignoreTimeScale)
		{
			feelDirection = Vector2.ClampMagnitude(feelDirection, clamp);
			ShakeInternal(feelDirection);
		}
	}

	public override void OnUIGameFeel(Vector2 feelDirection)
	{
		if (ignoreTimeScale)
		{
			feelDirection = Vector2.ClampMagnitude(feelDirection, clamp);
			ShakeInternal(feelDirection);
		}
	}
}
