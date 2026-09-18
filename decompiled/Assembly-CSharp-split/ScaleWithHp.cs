using UnityEngine;

public class ScaleWithHp : MonoBehaviour
{
	private DamagableEvent dmg;

	public AnimationCurve curve;

	private void Start()
	{
		dmg = GetComponentInParent<DamagableEvent>();
	}

	private void Update()
	{
		base.transform.localScale = Vector3.one * curve.Evaluate(dmg.currentHP);
	}
}
