using Sonigon;
using UnityEngine;

public class IkLeg : MonoBehaviour
{
	[Header("Sounds")]
	[SerializeField]
	private SoundEvent soundCharacterStep;

	[SerializeField]
	private SoundEvent soundCharacterStepBig;

	private SoundParameterIntensity soundParameterIntensity = new SoundParameterIntensity();

	private CharacterData data;

	private float legLenth;

	public Transform legRoot;

	public Transform footTarget;

	public LayerMask mask;

	public AnimationCurve upCurve;

	public AnimationCurve forwardCurve;

	public Transform moveDeltaTransform;

	[HideInInspector]
	public float stepTime;

	public float stepSpeed = 1f;

	private float footDownTime;

	[HideInInspector]
	public bool footDown = true;

	public IkLeg otherLeg;

	private Vector2 restPosition;

	private Vector2 startOffset;

	private Vector2 legRootOffset;

	private float scale = 1f;

	public float prediction = 1f;

	private Vector2 raycastPosLocal;

	private Vector2 raycastPosWorld;

	private Vector2 previousRaycastPosLocal;

	private Vector2 previousRaycastPosWorld;

	private Transform raycastTransform;

	private Transform previousRaycastTransform;

	private Vector2 footPosition;

	private Vector2 deltaPos;

	private Vector2 lastPos;

	private void Start()
	{
		data = GetComponentInParent<CharacterData>();
		legLenth = Vector3.Distance(base.transform.position, footTarget.position);
		startOffset = footTarget.position - data.transform.position;
		legRootOffset = legRoot.position - data.transform.position;
	}

	private void FixedUpdate()
	{
		if ((bool)data)
		{
			SetValuesFixed();
		}
	}

	private void LateUpdate()
	{
		if ((bool)data)
		{
			DoRayCast();
			UpdateRayCastWorldPos();
			DoStep();
			UpdatePreviousRayCastWorldPos();
			UpdateMiscVariables();
			SetFootPos();
			Apply();
		}
	}

	private void AirFootMovement()
	{
		footPosition = Vector3.Lerp(footPosition, restPosition + data.playerVel.velocity * 0.08f, TimeHandler.deltaTime * 15f);
		raycastTransform = null;
	}

	private bool stepWasLost()
	{
		if (Vector3.Distance(footPosition, legRoot.position) > legLenth * 0.5f)
		{
			return true;
		}
		return false;
	}

	private void DoStep()
	{
		if (footDown)
		{
			footDownTime += TimeHandler.deltaTime * stepSpeed / scale;
			if (!(footDownTime > 1f) || !otherLeg.footDown || (!(Mathf.Abs(data.playerVel.velocity.x) > 1f) && !stepWasLost()))
			{
				return;
			}
			StartStep();
			footDownTime = 0f;
			footDown = false;
			if (!data.dead && data.isPlaying && data.isGrounded && !data.isWallGrab)
			{
				soundParameterIntensity.intensity = Mathf.Abs(data.playerVel.velocity.x);
				if (data.stats.SoundTransformScaleThresholdReached())
				{
					SoundManager.Instance.Play(soundCharacterStepBig, data.transform, soundParameterIntensity);
				}
				else
				{
					SoundManager.Instance.Play(soundCharacterStep, data.transform, soundParameterIntensity);
				}
			}
		}
		else
		{
			stepTime += TimeHandler.deltaTime * stepSpeed / scale;
			if (stepTime > 1f)
			{
				EndStep();
				stepTime = 0f;
				footDown = true;
			}
		}
	}

	private void EndStep()
	{
		if ((bool)raycastTransform)
		{
			previousRaycastTransform = raycastTransform;
			previousRaycastPosLocal = raycastPosLocal;
		}
	}

	private void StartStep()
	{
	}

	private void SetFootPos()
	{
		if (data.isGrounded)
		{
			if ((bool)raycastTransform)
			{
				if (footDown)
				{
					footPosition = previousRaycastPosWorld;
				}
				else
				{
					footPosition = Vector3.Lerp(previousRaycastPosWorld, raycastPosWorld, forwardCurve.Evaluate(stepTime)) + Vector3.up * upCurve.Evaluate(stepTime);
				}
			}
		}
		else
		{
			AirFootMovement();
		}
	}

	private void Apply()
	{
		legRoot.position = (Vector2)data.transform.position + legRootOffset * scale;
		footTarget.position = footPosition;
	}

	private void DoRayCast()
	{
		Vector2 vector = deltaPos * prediction;
		Vector2 origin = data.transform.position + base.transform.right * base.transform.localPosition.x;
		Vector2 direction = Vector2.down + vector;
		float distance = legLenth * 1.5f * scale + vector.magnitude;
		RaycastHit2D[] array = Physics2D.RaycastAll(origin, direction, distance, mask);
		RaycastHit2D hit = default(RaycastHit2D);
		for (int i = 0; i < array.Length; i++)
		{
			if (!(array[i].transform.root == data.transform) && (bool)array[i].transform)
			{
				if (!hit.transform)
				{
					hit = array[i];
				}
				else if (array[i].distance < hit.distance)
				{
					hit = array[i];
				}
			}
		}
		if ((bool)hit.transform)
		{
			HitGround(hit);
		}
		else
		{
			HitNothing();
		}
	}

	private void HitNothing()
	{
	}

	private void HitGround(RaycastHit2D hit)
	{
		bool flag = false;
		if (!raycastTransform && (bool)hit.transform)
		{
			flag = true;
		}
		if ((bool)raycastTransform && raycastTransform != hit.transform)
		{
			MigrateGroundHit(raycastTransform, hit.transform, hit);
		}
		raycastTransform = hit.transform;
		raycastPosLocal = raycastTransform.InverseTransformPoint(hit.point);
		if (flag)
		{
			Land(hit);
		}
	}

	private void MigrateGroundHit(Transform from, Transform to, RaycastHit2D hit)
	{
	}

	private void Land(RaycastHit2D hit)
	{
		EndStep();
		UpdatePreviousRayCastWorldPos();
	}

	private void UpdateRayCastWorldPos()
	{
		if ((bool)raycastTransform)
		{
			raycastPosWorld = raycastTransform.TransformPoint(raycastPosLocal);
		}
	}

	private void UpdatePreviousRayCastWorldPos()
	{
		if ((bool)previousRaycastTransform)
		{
			previousRaycastPosWorld = previousRaycastTransform.TransformPoint(previousRaycastPosLocal);
		}
	}

	private void UpdateMiscVariables()
	{
		scale = data.transform.localScale.x;
		restPosition = (Vector2)data.transform.position + startOffset * 0.7f * scale;
	}

	private void SetValuesFixed()
	{
		if ((bool)moveDeltaTransform)
		{
			deltaPos = Vector3.Lerp(deltaPos, (Vector2)moveDeltaTransform.position - lastPos, TimeHandler.deltaTime * 15f);
			lastPos = moveDeltaTransform.position;
			deltaPos.y = 0f;
		}
	}
}
