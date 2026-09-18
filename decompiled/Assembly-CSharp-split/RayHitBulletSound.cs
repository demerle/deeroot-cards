using Sonigon;
using UnityEngine;

public class RayHitBulletSound : RayHitEffect
{
	[Header("Sound Settings")]
	public bool disableImpact;

	public bool playLocalImpact;

	public SoundEvent soundLocalImpact;

	public bool localImpactVelocityToIntensity;

	private ProjectileHit projectileHit;

	private RayHitReflect rayHitReflect;

	private MoveTransform moveTransform;

	private SoundParameterIntensity soundIntensity = new SoundParameterIntensity();

	private void Start()
	{
		projectileHit = GetComponent<ProjectileHit>();
		rayHitReflect = GetComponent<RayHitReflect>();
		moveTransform = GetComponent<MoveTransform>();
	}

	public override HasToReturn DoHitEffect(HitInfo hit)
	{
		if (disableImpact)
		{
			return HasToReturn.canContinue;
		}
		if (localImpactVelocityToIntensity)
		{
			soundIntensity.intensity = moveTransform.velocity.magnitude;
		}
		if (playLocalImpact)
		{
			if (soundLocalImpact != null && hit.collider != null && hit.collider.tag != "Player")
			{
				if (localImpactVelocityToIntensity)
				{
					SoundManager.Instance.PlayAtPosition(soundLocalImpact, SoundManager.Instance.GetTransform(), hit.point, soundIntensity);
				}
				else
				{
					SoundManager.Instance.PlayAtPosition(soundLocalImpact, SoundManager.Instance.GetTransform(), hit.point);
				}
			}
		}
		else
		{
			projectileHit.ownPlayer.data.weaponHandler.gun.soundGun.PlayImpact(hit, rayHitReflect);
		}
		return HasToReturn.canContinue;
	}
}
