using UnityEngine;

public class MoveTransformGravity : MonoBehaviour
{
	public float amount = 1f;

	public float pow = 1.5f;

	private MoveTransform move;

	private float counter;

	private void Start()
	{
		move = GetComponentInParent<MoveTransform>();
	}

	private void Update()
	{
		counter += TimeHandler.deltaTime;
		move.velocity += Vector3.down * Mathf.Pow(amount * counter, pow);
	}
}
