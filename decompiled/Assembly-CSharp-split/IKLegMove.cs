using UnityEngine;

public class IKLegMove : MonoBehaviour
{
	private Rigidbody2D rig;

	public bool touchesGround;

	public IKLegMove otherLeg;

	public Transform target;

	public Transform foot;

	private Vector3 transformGroundPos;

	private Vector3 currentTransformGroundPos;

	private Vector3 previousTransformGroundPos;

	private Transform hitTransform;

	private Transform currentHitTransform;

	[HideInInspector]
	public float allowedFootDistance;

	private float defaultAllowedFootDistance;

	public AnimationCurve footCurveUp;

	private float distanceToFoot;

	public LayerMask mask;

	[HideInInspector]
	public float animationTime = 1f;

	private Vector3 velocity;

	private CharacterData data;

	private float animationLength;

	private bool hasLanded;

	private void Start()
	{
		rig = GetComponentInParent<Rigidbody2D>();
		data = GetComponentInParent<CharacterData>();
		defaultAllowedFootDistance = Vector3.Distance(base.transform.position, foot.position);
	}

	private void LateUpdate()
	{
		allowedFootDistance = (defaultAllowedFootDistance = base.transform.root.localScale.x);
		animationLength = footCurveUp.keys[footCurveUp.keys.Length - 1].time;
		if (!data.isGrounded)
		{
			animationTime = 1f;
			hasLanded = false;
		}
		distanceToFoot = Vector3.Distance(base.transform.position + (Vector3)rig.velocity * 0.01f, target.position);
		velocity = rig.velocity;
		if (velocity.y > 0f)
		{
			velocity.y *= -1f;
		}
		animationTime += TimeHandler.deltaTime;
		RayCastForFootPosition();
		touchesGround = footCurveUp.Evaluate(animationTime) < 0.1f;
		DoSteps();
		MoveFoot();
	}

	private void DoSteps()
	{
		float num = 0f;
		if (target.position.x > base.transform.position.x == rig.velocity.x > 0f)
		{
			num += Mathf.Abs(rig.velocity.x) * 0.25f;
		}
		float num2 = Mathf.Clamp(animationTime * 0.4f - 0.23f, 0f, 0.2f);
		if ((distanceToFoot > allowedFootDistance + num - num2 && data.isGrounded && (otherLeg.animationTime > animationLength * 0.5f || animationLength > 2f) && animationTime > animationLength) || !hasLanded)
		{
			hasLanded = true;
			currentHitTransform = hitTransform;
			previousTransformGroundPos = currentTransformGroundPos;
			currentTransformGroundPos = transformGroundPos;
			animationTime = 0f;
		}
		else
		{
			Vector3 vector = transformGroundPos - currentTransformGroundPos;
			vector *= Mathf.Clamp(footCurveUp.Evaluate(animationTime) * 1f, 0f, 1f);
			previousTransformGroundPos += vector;
			currentTransformGroundPos += vector;
		}
	}

	private void MoveFoot()
	{
		if (!data.isGrounded)
		{
			target.position = Vector3.Lerp(target.position, base.transform.position + Vector3.down * allowedFootDistance * 0.5f, TimeHandler.deltaTime * 1f);
			target.position += (Vector3)rig.velocity * 0.1f * TimeHandler.deltaTime;
		}
		else if ((bool)currentHitTransform)
		{
			Vector3 vector = currentHitTransform.TransformPoint(currentTransformGroundPos);
			Vector3 a = currentHitTransform.TransformPoint(previousTransformGroundPos);
			Vector3 vector2 = vector;
			float num = 0f;
			if (animationTime < animationLength)
			{
				vector2 = Vector3.Lerp(a, vector, animationTime / animationLength);
				num = footCurveUp.Evaluate(animationTime);
			}
			target.position = vector2 + Vector3.up * num;
		}
	}

	private void RayCastForFootPosition()
	{
		RaycastHit2D[] array = Physics2D.RaycastAll(base.transform.position, Vector3.down + velocity * 0.2f, 3f, mask);
		RaycastHit2D raycastHit2D = default(RaycastHit2D);
		float num = float.PositiveInfinity;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].transform.root != base.transform.root && array[i].distance < num)
			{
				raycastHit2D = array[i];
				num = array[i].distance;
			}
		}
		if ((bool)raycastHit2D.transform)
		{
			hitTransform = raycastHit2D.transform;
			transformGroundPos = hitTransform.InverseTransformPoint(raycastHit2D.point);
		}
	}
}
