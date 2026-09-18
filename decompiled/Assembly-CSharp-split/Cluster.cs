using UnityEngine;

public class Cluster : MonoBehaviour
{
	public float distanceToTravel = 8f;

	public float spread;

	public float velocitySpread;

	public int clusters = 3;

	private MoveTransform move;

	private void Start()
	{
		move = GetComponentInParent<MoveTransform>();
	}

	private void Update()
	{
		if (!(move.distanceTravelled > distanceToTravel))
		{
			return;
		}
		for (int i = 0; i < clusters; i++)
		{
			GameObject obj = Object.Instantiate(base.transform.root.gameObject, base.transform.root.position, base.transform.root.rotation);
			Cluster componentInChildren = obj.GetComponentInChildren<Cluster>();
			if ((bool)componentInChildren)
			{
				Object.Destroy(componentInChildren);
			}
			MoveTransform component = obj.GetComponent<MoveTransform>();
			if ((bool)component)
			{
				component.DontRunStart = true;
				component.velocity = base.transform.root.GetComponent<MoveTransform>().velocity;
				component.multiplier = base.transform.root.GetComponent<MoveTransform>().multiplier;
				component.velocity += base.transform.right * Random.Range(0f - spread, spread);
				component.velocity *= Random.Range(1f - velocitySpread * 0.01f, 1f + velocitySpread * 0.01f);
			}
		}
		Object.Destroy(this);
	}
}
