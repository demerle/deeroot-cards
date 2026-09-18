using Sonigon;
using UnityEngine;

public class TrickShot : MonoBehaviour
{
	[Header("Sound")]
	public SoundEvent soundGrowExplosion;

	public SoundEvent soundGrowWail;

	private bool soundGrowExplosionPlayed;

	private bool soundGrowWailPlayed;

	private SoundParameterIntensity soundIntensity = new SoundParameterIntensity(0f);

	[Header("Settings")]
	public float muiltiplier = 1f;

	public float removeAt = 30f;

	private ProjectileHit projectileHit;

	private MoveTransform move;

	private ScaleTrailFromDamage trail;

	private float lastDistanceTravelled;

	private void Awake()
	{
		trail = base.transform.root.GetComponentInChildren<ScaleTrailFromDamage>();
	}

	private void Start()
	{
		projectileHit = GetComponentInParent<ProjectileHit>();
		move = GetComponentInParent<MoveTransform>();
		if (projectileHit != null)
		{
			if (soundGrowExplosion != null)
			{
				projectileHit.AddHitActionWithData(SoundPlayGrowExplosion);
			}
			if (soundGrowWail != null)
			{
				soundGrowWailPlayed = true;
				SoundManager.Instance.Play(soundGrowWail, projectileHit.ownPlayer.transform);
			}
		}
	}

	public void SoundPlayGrowExplosion(HitInfo hit)
	{
		if (!soundGrowExplosionPlayed)
		{
			soundGrowExplosionPlayed = true;
			if (soundGrowExplosion != null)
			{
				SoundManager.Instance.PlayAtPosition(soundGrowExplosion, projectileHit.ownPlayer.transform, hit.point, soundIntensity);
			}
			if (soundGrowWailPlayed)
			{
				SoundManager.Instance.Stop(soundGrowWail, projectileHit.ownPlayer.transform);
			}
		}
	}

	private void Update()
	{
		if (move.distanceTravelled > removeAt)
		{
			Object.Destroy(this);
			return;
		}
		soundIntensity.intensity = move.distanceTravelled / removeAt;
		float num = move.distanceTravelled - lastDistanceTravelled;
		lastDistanceTravelled = move.distanceTravelled;
		float num2 = 1f + num * TimeHandler.deltaTime * base.transform.localScale.x * muiltiplier;
		projectileHit.damage *= num2;
		projectileHit.shake *= num2;
		if ((bool)trail)
		{
			trail.Rescale();
		}
	}
}
