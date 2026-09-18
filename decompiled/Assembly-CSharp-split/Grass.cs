using UnityEngine;

public class Grass : MonoBehaviour
{
	public float target;

	private float velocity;

	private float currentRot;

	public float minSpring;

	public float maxSpring;

	public float minDrag;

	public float maxDrag;

	private float spring;

	private float drag;

	public float minPlayerEffect;

	public float maxPlayerEffect;

	private float playerEffect;

	public Sprite[] sprites;

	private void Start()
	{
		if (Random.value > 0.5f)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		GetComponentInChildren<SpriteRenderer>().sprite = sprites[Random.Range(0, sprites.Length)];
		spring = Random.Range(minSpring, maxSpring);
		drag = Random.Range(minDrag, maxDrag);
		playerEffect = Random.Range(minPlayerEffect, maxPlayerEffect);
		currentRot = base.transform.localEulerAngles.z;
	}

	private void Update()
	{
		velocity = Mathf.Lerp(velocity, (target - currentRot) * spring, CappedDeltaTime.time * drag);
		currentRot += velocity * CappedDeltaTime.time;
		base.transform.localEulerAngles = new Vector3(0f, 0f, currentRot);
		if (PlayerManager.instance.players != null && PlayerManager.instance.players.Count > 0 && Vector2.Distance(PlayerManager.instance.players[0].data.playerVel.position, base.transform.position) < 1.5f)
		{
			AddForce(PlayerManager.instance.players[0].data.playerVel.velocity.x * playerEffect * CappedDeltaTime.time * 60f);
		}
		float num = 0.5f;
		float num2 = 0.05f;
		AddForce((Mathf.PerlinNoise(base.transform.position.x * num2 + Time.time * num, base.transform.position.y * num2 + Time.time * num) - 0.5f) * 30f * playerEffect * CappedDeltaTime.time * 60f);
	}

	public void AddForce(float f)
	{
		velocity += f;
	}
}
