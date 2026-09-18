using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class ChomaticAberrationFeeler : GameFeeler
{
	public float force = 10f;

	public float damper = 0.95f;

	public float damperSpring = 0.95f;

	public float threshold;

	private PostProcessVolume postProcessVolume;

	private ChromaticAberration chromaticAberration;

	private float intensity;

	private float velocity;

	private float targetIntensity;

	public override void OnAwake()
	{
		postProcessVolume = GetComponent<PostProcessVolume>();
		if (!postProcessVolume.profile.TryGetSettings<ChromaticAberration>(out chromaticAberration))
		{
			Debug.LogError("No ChromaticAberration in post!");
		}
		else
		{
			targetIntensity = chromaticAberration.intensity.value;
		}
	}

	public override void OnGameFeel(Vector2 feelDirection)
	{
		feelDirection = Vector2.ClampMagnitude(feelDirection, 50f);
		if (feelDirection.magnitude < threshold)
		{
			feelDirection = feelDirection.normalized * threshold * 0.3f;
		}
		velocity += feelDirection.sqrMagnitude * 0.2f * force;
	}

	private void Update()
	{
		velocity *= damper;
		intensity *= damperSpring;
		intensity += velocity * Mathf.Clamp(TimeHandler.deltaTime, 0f, 0.02f);
		chromaticAberration.intensity.value = intensity + targetIntensity;
	}

	private void FixedUpdate()
	{
		intensity *= 0.9f;
	}

	public override void OnUIGameFeel(Vector2 feelDirection)
	{
	}
}
