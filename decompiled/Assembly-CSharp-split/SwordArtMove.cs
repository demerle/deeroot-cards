using UnityEngine;

public class SwordArtMove : MonoBehaviour
{
	private MoveTransform move;

	public LayerMask mask;

	public AnimationCurve heightCurve;

	public AnimationCurve AwayCurve;

	private Vector3 startUp;

	private Vector3 startForward;

	private Vector3 startPos;

	public float multiplier = 1f;

	public float heightMultiplier = 1f;

	public float awayMultiplier = 1f;

	public float speed = 1f;

	private float counter;

	private ParticleSystem[] parts;

	private void Start()
	{
		parts = GetComponentsInChildren<ParticleSystem>();
		move = GetComponentInParent<MoveTransform>();
		move.enabled = false;
		startForward = base.transform.forward;
		startUp = Vector3.Cross(base.transform.forward, Vector3.forward);
		startPos = base.transform.position;
		awayMultiplier *= Mathf.Pow(move.localForce.magnitude / 40f * 2f, 0.45f);
		awayMultiplier /= 1f + move.drag * 0.05f;
		speed *= Mathf.Pow(awayMultiplier, 0.3f);
	}

	private void Update()
	{
		counter += TimeHandler.deltaTime * speed;
		Vector3 vector = startPos + (heightCurve.Evaluate(counter) * heightMultiplier * startUp + AwayCurve.Evaluate(counter) * awayMultiplier * startForward) * multiplier;
		RaycastHit2D raycastHit2D = Physics2D.Raycast(startPos, (vector - startPos).normalized, Vector3.Distance(vector, startPos), mask);
		if ((bool)raycastHit2D.transform && !raycastHit2D.collider.GetComponent<Damagable>())
		{
			vector = raycastHit2D.point + raycastHit2D.normal * 0.1f;
			for (int i = 0; i < parts.Length; i++)
			{
				parts[i].transform.position = (Vector3)raycastHit2D.point + Vector3.forward * 8f;
				parts[i].transform.rotation = Quaternion.LookRotation(raycastHit2D.normal);
				parts[i].Emit(1);
			}
		}
		base.transform.root.position = vector;
	}
}
