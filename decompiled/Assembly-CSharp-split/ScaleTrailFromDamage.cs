using UnityEngine;

public class ScaleTrailFromDamage : MonoBehaviour
{
	private float startWidth;

	private float startTime;

	private void Start()
	{
		float num = 55f;
		ProjectileHit componentInParent = GetComponentInParent<ProjectileHit>();
		GetComponentInParent<RayCastTrail>();
		if ((bool)componentInParent)
		{
			num = componentInParent.damage;
		}
		TrailRenderer componentInChildren = GetComponentInChildren<TrailRenderer>();
		startWidth = componentInChildren.widthMultiplier;
		startTime = componentInChildren.time;
		if ((bool)componentInChildren)
		{
			componentInChildren.widthMultiplier *= (1f + num / 55f) / 2f;
			componentInChildren.time *= Mathf.Clamp((1f + num / 55f) / 2f, 0f, 25f);
			if (num > 100f)
			{
				componentInChildren.numCapVertices = 5;
			}
			if (num > 500f)
			{
				componentInChildren.numCapVertices = 10;
			}
		}
	}

	public void Rescale()
	{
		float num = 55f;
		ProjectileHit componentInParent = GetComponentInParent<ProjectileHit>();
		RayCastTrail componentInParent2 = GetComponentInParent<RayCastTrail>();
		if ((bool)componentInParent)
		{
			num = componentInParent.damage;
		}
		TrailRenderer componentInChildren = GetComponentInChildren<TrailRenderer>();
		if ((bool)componentInChildren)
		{
			_ = componentInParent2.extraSize;
			componentInChildren.widthMultiplier = startWidth * ((1f + num / 55f) / 2f);
			componentInChildren.time = startTime * Mathf.Clamp((1f + num / 55f) / 2f, 0f, 25f);
			if (num > 100f)
			{
				componentInChildren.numCapVertices = 5;
			}
			if (num > 100f)
			{
				componentInChildren.numCapVertices = 10;
			}
		}
	}
}
