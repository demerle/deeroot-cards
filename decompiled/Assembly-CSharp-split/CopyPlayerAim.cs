using UnityEngine;

public class CopyPlayerAim : MonoBehaviour
{
	public float spreadPerLevel;

	public float spread;

	private AttackLevel level;

	private void Start()
	{
		level = GetComponentInParent<AttackLevel>();
	}

	public void Go()
	{
		float num = Random.Range(0f - spread, spread);
		float num2 = 0f;
		if ((bool)level)
		{
			num2 = (float)level.attackLevel * spreadPerLevel;
		}
		num += Random.Range(0f - num2, num2);
		Holding component = base.transform.root.GetComponent<Holding>();
		if ((bool)component)
		{
			base.transform.rotation = component.holdable.GetComponentInChildren<ShootPos>().transform.rotation;
		}
		base.transform.Rotate(Vector3.Cross(Vector3.forward, base.transform.forward) * num);
	}
}
