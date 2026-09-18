using UnityEngine;

public class BulletDamageEvents : MonoBehaviour
{
	public DmgEvent[] damageEvents;

	public void Start()
	{
		ProjectileHit componentInParent = GetComponentInParent<ProjectileHit>();
		for (int i = 0; i < damageEvents.Length; i++)
		{
			if (componentInParent.damage > damageEvents[i].dmg)
			{
				damageEvents[i].eventToCall.Invoke();
			}
		}
	}
}
