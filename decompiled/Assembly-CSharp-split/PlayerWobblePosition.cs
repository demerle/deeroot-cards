using UnityEngine;

public class PlayerWobblePosition : MonoBehaviour
{
	private Vector3 physicsPos;

	public float drag = 15f;

	public float spring = 1000f;

	public float multiplier = 1f;

	public float prediction;

	private Vector3 velocity;

	private Player player;

	private void Start()
	{
		physicsPos = base.transform.position;
		player = GetComponentInParent<Player>();
	}

	private void Update()
	{
		float num = Mathf.Clamp(TimeHandler.deltaTime, 0f, 0.03f);
		Vector3 position = player.transform.position;
		if (prediction > 0f)
		{
			position += (Vector3)player.data.playerVel.velocity * prediction;
		}
		velocity += (position - physicsPos) * num * spring;
		velocity -= velocity * drag * num;
		physicsPos += num * multiplier * velocity;
		base.transform.position = physicsPos;
	}
}
