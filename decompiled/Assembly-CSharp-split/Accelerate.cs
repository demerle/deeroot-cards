using UnityEngine;

public class Accelerate : MonoBehaviour
{
	private MoveTransform move;

	public float startMultiplier = 0.5f;

	public float acceleratonPerSecond = 2f;

	public float pow = 1f;

	private void Start()
	{
		move = GetComponentInParent<MoveTransform>();
		move.multiplier *= startMultiplier;
		move.multiplier = Mathf.Clamp(move.multiplier, 0.01f, float.PositiveInfinity);
	}

	private void Update()
	{
		move.multiplier = Mathf.Clamp(move.multiplier + TimeHandler.deltaTime * acceleratonPerSecond * Mathf.Pow(move.multiplier, pow), 0f, 25f);
	}
}
