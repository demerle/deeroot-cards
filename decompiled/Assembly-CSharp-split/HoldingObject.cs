using UnityEngine;

public class HoldingObject : MonoBehaviour
{
	public Holding holder;

	private void Start()
	{
	}

	private void Update()
	{
		if (CardChoice.instance.IsPicking)
		{
			base.transform.position = Vector3.up * 10000f;
		}
		else if (Vector3.Distance(holder.transform.position, base.transform.position) > 100f)
		{
			base.transform.position = holder.transform.position;
		}
	}
}
