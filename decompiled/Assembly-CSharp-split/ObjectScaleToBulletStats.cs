using UnityEngine;

public class ObjectScaleToBulletStats : MonoBehaviour
{
	public GameObject target;

	public float dmgAmount = 1f;

	public float speedAmount = 1f;

	private void Start()
	{
		ProjectileHit component = GetComponent<ProjectileHit>();
		MoveTransform component2 = GetComponent<MoveTransform>();
		if ((bool)component)
		{
			component.damage = Mathf.Lerp(component.damage, component.damage * target.transform.localScale.x, dmgAmount);
		}
		if ((bool)component2)
		{
			component2.localForce.z = Mathf.Lerp(component2.localForce.z, component2.localForce.z * target.transform.localScale.x, speedAmount);
		}
	}
}
