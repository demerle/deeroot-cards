using UnityEngine;

public class RescaleFromDamage : MonoBehaviour
{
	private ProjectileHit hit;

	private float startScale;

	private void Start()
	{
		startScale = base.transform.localScale.x;
	}

	public void Rescale()
	{
		if (!hit)
		{
			hit = GetComponentInParent<ProjectileHit>();
		}
		if ((bool)hit)
		{
			base.transform.localScale = Vector3.one * startScale * (hit.damage / 55f);
		}
	}
}
