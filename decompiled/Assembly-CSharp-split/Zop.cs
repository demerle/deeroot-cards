using UnityEngine;

public class Zop : MonoBehaviour
{
	public float turn = 0.2f;

	private float count;

	private int sinceSwitch = 1;

	private bool up;

	private MoveTransform move;

	public bool randomZop;

	private void Start()
	{
		move = GetComponentInParent<MoveTransform>();
		if (base.transform.forward.x < 0f)
		{
			up = true;
		}
	}

	private void Update()
	{
		count += TimeHandler.deltaTime;
		if (!(count > turn))
		{
			return;
		}
		if (up)
		{
			move.velocity = move.velocity.magnitude * Vector3.Cross(base.transform.forward, Vector3.forward);
		}
		else
		{
			move.velocity = move.velocity.magnitude * -Vector3.Cross(base.transform.forward, Vector3.forward);
		}
		count = 0f;
		if (!randomZop || !(Random.value > 0.8f))
		{
			sinceSwitch++;
			if (sinceSwitch == 2)
			{
				up = !up;
				sinceSwitch = 0;
			}
		}
	}
}
