using UnityEngine;

public class LegRaycasters : MonoBehaviour
{
	public LayerMask mask;

	public float force;

	public float drag;

	public Transform[] legCastPositions;

	public AnimationCurve animationCurve;

	private PlayerVelocity rig;

	private CharacterData data;

	public AnimationCurve wobbleCurve;

	public AnimationCurve forceCurve;

	private IkLeg[] legs;

	private float totalStepTime;

	private void Awake()
	{
		legs = base.transform.root.GetComponentsInChildren<IkLeg>();
	}

	private void Start()
	{
		rig = GetComponentInParent<PlayerVelocity>();
		data = GetComponentInParent<CharacterData>();
	}

	private void FixedUpdate()
	{
		totalStepTime = 0f;
		for (int i = 0; i < legs.Length; i++)
		{
			if (!legs[i].footDown)
			{
				totalStepTime += legs[i].stepTime;
			}
		}
		for (int j = 0; j < legCastPositions.Length; j++)
		{
			RaycastHit2D[] array = Physics2D.RaycastAll(legCastPositions[j].transform.position + Vector3.up * 0.5f, Vector2.down, 1f * base.transform.root.localScale.x, mask);
			for (int k = 0; k < array.Length; k++)
			{
				if ((bool)array[k].transform && array[k].transform.root != base.transform.root)
				{
					HitGround(legCastPositions[j], array[k]);
					break;
				}
			}
		}
	}

	private void HitGround(Transform leg, RaycastHit2D hit)
	{
		if (!(data.sinceJump < 0.2f) && !(Vector3.Angle(Vector3.up, hit.normal) > 70f))
		{
			data.TouchGround(hit.point, hit.normal, hit.rigidbody);
			Vector3 vector = ((Vector3)hit.point - leg.transform.position) / base.transform.root.localScale.x;
			if (data.input.direction.x != 0f)
			{
				vector.y += wobbleCurve.Evaluate(totalStepTime) * base.transform.root.localScale.x;
				rig.AddForce(Vector3.up * forceCurve.Evaluate(totalStepTime) * rig.mass);
			}
			rig.AddForce(animationCurve.Evaluate(Mathf.Abs(vector.y)) * Vector3.up * rig.mass * force);
			rig.AddForce(animationCurve.Evaluate(Mathf.Abs(vector.y)) * (0f - rig.velocity.y) * Vector2.up * rig.mass * drag);
		}
	}
}
