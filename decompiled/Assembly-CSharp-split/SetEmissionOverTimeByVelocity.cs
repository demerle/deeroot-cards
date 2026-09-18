using UnityEngine;

public class SetEmissionOverTimeByVelocity : MonoBehaviour
{
	private ParticleSystem part;

	public AnimationCurve curve;

	private PlayerVelocity rig;

	private void Start()
	{
		rig = GetComponentInParent<PlayerVelocity>();
		part = GetComponent<ParticleSystem>();
	}

	private void Update()
	{
		ParticleSystem.EmissionModule emission = part.emission;
		emission.rateOverDistance = curve.Evaluate(rig.velocity.magnitude);
	}
}
