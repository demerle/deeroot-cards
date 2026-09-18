using UnityEngine;

public class ZapEffect : MonoBehaviour
{
	public float damage;

	public float range = 1f;

	private void Start()
	{
		damage *= base.transform.localScale.x;
		range *= (1f + base.transform.localScale.x) * 0.5f;
		Player closestPlayer = PlayerManager.instance.GetClosestPlayer(base.transform.position, needVision: true);
		if ((bool)closestPlayer && Vector3.Distance(base.transform.position, closestPlayer.transform.position) < range)
		{
			closestPlayer.data.healthHandler.TakeDamage(damage * (closestPlayer.transform.position - base.transform.position).normalized, base.transform.position, null, PlayerManager.instance.GetOtherPlayer(closestPlayer));
			GetComponentInChildren<LineEffect>(includeInactive: true).Play(base.transform, closestPlayer.transform, 2f);
		}
	}
}
