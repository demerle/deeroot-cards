using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Animations;

namespace RootMotion.FinalIK;

public struct CCDIKJob : IAnimationJob
{
	public TransformSceneHandle _target;

	public PropertySceneHandle _IKPositionWeight;

	public PropertySceneHandle _maxIterations;

	public PropertySceneHandle _tolerance;

	public PropertySceneHandle _XY;

	public PropertySceneHandle _useRotationLimits;

	private NativeArray<TransformStreamHandle> bones;

	private NativeArray<PropertySceneHandle> boneWeights;

	private NativeArray<float> boneSqrMags;

	private float chainSqrMag;

	private Vector3 lastLocalDirection;

	private NativeArray<Quaternion> limitDefaultLocalRotationArray;

	private NativeArray<Vector3> limitAxisArray;

	private NativeArray<int> hingeFlags;

	private NativeArray<PropertySceneHandle> hingeMinArray;

	private NativeArray<PropertySceneHandle> hingeMaxArray;

	private NativeArray<PropertySceneHandle> hingeUseLimitsArray;

	private NativeArray<Quaternion> hingeLastRotations;

	private NativeArray<float> hingeLastAngles;

	private NativeArray<int> angleFlags;

	private NativeArray<Vector3> angleSecondaryAxisArray;

	private NativeArray<PropertySceneHandle> angleLimitArray;

	private NativeArray<PropertySceneHandle> angleTwistLimitArray;

	public void Setup(Animator animator, Transform[] bones, Transform target)
	{
		this.bones = new NativeArray<TransformStreamHandle>(bones.Length, Allocator.Persistent);
		boneWeights = new NativeArray<PropertySceneHandle>(bones.Length - 1, Allocator.Persistent);
		boneSqrMags = new NativeArray<float>(bones.Length - 1, Allocator.Persistent);
		for (int i = 0; i < this.bones.Length; i++)
		{
			this.bones[i] = animator.BindStreamTransform(bones[i]);
		}
		for (int j = 0; j < this.bones.Length - 1; j++)
		{
			if (bones[j].gameObject.GetComponent<IKJBoneParams>() == null)
			{
				bones[j].gameObject.AddComponent<IKJBoneParams>();
			}
			boneWeights[j] = animator.BindSceneProperty(bones[j].transform, typeof(IKJBoneParams), "weight");
		}
		SetUpRotationLimits(animator, bones);
		_target = animator.BindSceneTransform(target);
		_IKPositionWeight = animator.BindSceneProperty(animator.transform, typeof(CCDIKJ), "weight");
		_maxIterations = animator.BindSceneProperty(animator.transform, typeof(CCDIKJ), "maxIterations");
		_tolerance = animator.BindSceneProperty(animator.transform, typeof(CCDIKJ), "tolerance");
		_XY = animator.BindSceneProperty(animator.transform, typeof(CCDIKJ), "XY");
		_useRotationLimits = animator.BindSceneProperty(animator.transform, typeof(CCDIKJ), "useRotationLimits");
	}

	private void SetUpRotationLimits(Animator animator, Transform[] bones)
	{
		limitDefaultLocalRotationArray = new NativeArray<Quaternion>(bones.Length, Allocator.Persistent);
		limitAxisArray = new NativeArray<Vector3>(bones.Length, Allocator.Persistent);
		hingeFlags = new NativeArray<int>(bones.Length, Allocator.Persistent);
		hingeMinArray = new NativeArray<PropertySceneHandle>(bones.Length, Allocator.Persistent);
		hingeMaxArray = new NativeArray<PropertySceneHandle>(bones.Length, Allocator.Persistent);
		hingeUseLimitsArray = new NativeArray<PropertySceneHandle>(bones.Length, Allocator.Persistent);
		hingeLastRotations = new NativeArray<Quaternion>(bones.Length, Allocator.Persistent);
		hingeLastAngles = new NativeArray<float>(bones.Length, Allocator.Persistent);
		angleFlags = new NativeArray<int>(bones.Length, Allocator.Persistent);
		angleSecondaryAxisArray = new NativeArray<Vector3>(bones.Length, Allocator.Persistent);
		angleLimitArray = new NativeArray<PropertySceneHandle>(bones.Length, Allocator.Persistent);
		angleTwistLimitArray = new NativeArray<PropertySceneHandle>(bones.Length, Allocator.Persistent);
		for (int i = 0; i < bones.Length - 1; i++)
		{
			hingeFlags[i] = 0;
			angleFlags[i] = 0;
			RotationLimit component = bones[i].GetComponent<RotationLimit>();
			if (component != null)
			{
				limitDefaultLocalRotationArray[i] = bones[i].localRotation;
				limitAxisArray[i] = component.axis;
				component.Disable();
				if (component is RotationLimitHinge)
				{
					hingeFlags[i] = 1;
					hingeMinArray[i] = animator.BindSceneProperty(bones[i].transform, typeof(RotationLimitHinge), "min");
					hingeMaxArray[i] = animator.BindSceneProperty(bones[i].transform, typeof(RotationLimitHinge), "max");
					hingeUseLimitsArray[i] = animator.BindSceneProperty(bones[i].transform, typeof(RotationLimitHinge), "useLimits");
					hingeLastRotations[i] = bones[i].localRotation;
					hingeLastAngles[i] = 0f;
				}
				if (component is RotationLimitAngle)
				{
					RotationLimitAngle rotationLimitAngle = component as RotationLimitAngle;
					angleFlags[i] = 1;
					angleSecondaryAxisArray[i] = rotationLimitAngle.secondaryAxis;
					angleLimitArray[i] = animator.BindSceneProperty(bones[i].transform, typeof(RotationLimitAngle), "limit");
					angleTwistLimitArray[i] = animator.BindSceneProperty(bones[i].transform, typeof(RotationLimitAngle), "twistLimit");
				}
			}
		}
	}

	private void DisposeRotationLimits()
	{
		limitDefaultLocalRotationArray.Dispose();
		limitAxisArray.Dispose();
		hingeFlags.Dispose();
		hingeMinArray.Dispose();
		hingeMaxArray.Dispose();
		hingeUseLimitsArray.Dispose();
		hingeLastRotations.Dispose();
		hingeLastAngles.Dispose();
		angleFlags.Dispose();
		angleSecondaryAxisArray.Dispose();
		angleLimitArray.Dispose();
		angleTwistLimitArray.Dispose();
	}

	public void ProcessRootMotion(AnimationStream stream)
	{
	}

	public void ProcessAnimation(AnimationStream stream)
	{
		Update(stream);
	}

	private void Update(AnimationStream s)
	{
		if (!_target.IsValid(s))
		{
			return;
		}
		float num = _IKPositionWeight.GetFloat(s);
		if (num <= 0f)
		{
			return;
		}
		num = Mathf.Min(num, 1f);
		Read(s);
		bool flag = _XY.GetBool(s);
		float num2 = _maxIterations.GetInt(s);
		float num3 = _tolerance.GetFloat(s);
		bool useRotationLimits = _useRotationLimits.GetBool(s);
		Vector3 position = _target.GetPosition(s);
		if (flag)
		{
			position.z = bones[0].GetPosition(s).z;
		}
		Vector3 vector = ((num2 > 1f) ? GetSingularityOffset(s, position, useRotationLimits) : Vector3.zero);
		int num4 = 1;
		for (int i = 0; (float)i < num2; i++)
		{
			Vector3 localDirection = GetLocalDirection(s);
			if (vector == Vector3.zero && i >= 1 && num3 > 0f && GetPositionOffset(s, localDirection) < num3 * num3)
			{
				break;
			}
			lastLocalDirection = localDirection;
			Solve(s, position + ((i == 0) ? vector : Vector3.zero), flag, num, num4, useRotationLimits);
			num4++;
			if (num4 >= bones.Length - 1)
			{
				num4 -= bones.Length - 2;
			}
		}
		lastLocalDirection = GetLocalDirection(s);
	}

	private void Read(AnimationStream s)
	{
		chainSqrMag = 0f;
		for (int i = 0; i < bones.Length; i++)
		{
			if (i < bones.Length - 1)
			{
				boneSqrMags[i] = (bones[i].GetPosition(s) - bones[i + 1].GetPosition(s)).sqrMagnitude;
				chainSqrMag += boneSqrMags[i];
			}
		}
	}

	private void Solve(AnimationStream s, Vector3 targetPosition, bool XY, float weight, int it, bool useRotationLimits)
	{
		for (int num = bones.Length - 2; num > -1; num--)
		{
			float num2 = weight * boneWeights[num].GetFloat(s);
			if (num2 > 0f)
			{
				Vector3 position = bones[num].GetPosition(s);
				Vector3 fromDirection = bones[bones.Length - 1].GetPosition(s) - position;
				Vector3 toDirection = targetPosition - position;
				if (XY)
				{
					float current = Mathf.Atan2(fromDirection.x, fromDirection.y) * 57.29578f;
					float target = Mathf.Atan2(toDirection.x, toDirection.y) * 57.29578f;
					bones[num].SetRotation(s, Quaternion.AngleAxis(Mathf.DeltaAngle(current, target) * num2, Vector3.back) * bones[num].GetRotation(s));
				}
				else
				{
					Quaternion rotation = bones[num].GetRotation(s);
					Quaternion quaternion = Quaternion.FromToRotation(fromDirection, toDirection) * rotation;
					if (num2 >= 1f)
					{
						bones[num].SetRotation(s, quaternion);
					}
					else
					{
						bones[num].SetRotation(s, Quaternion.Lerp(rotation, quaternion, num2));
					}
				}
			}
			if (useRotationLimits)
			{
				if (hingeFlags[num] == 1)
				{
					Quaternion rotation2 = Quaternion.Inverse(limitDefaultLocalRotationArray[num]) * bones[num].GetLocalRotation(s);
					Quaternion lastRotation = hingeLastRotations[num];
					float lastAngle = hingeLastAngles[num];
					Quaternion quaternion2 = RotationLimitUtilities.LimitHinge(rotation2, hingeMinArray[num].GetFloat(s), hingeMaxArray[num].GetFloat(s), hingeUseLimitsArray[num].GetBool(s), limitAxisArray[num], ref lastRotation, ref lastAngle);
					hingeLastRotations[num] = lastRotation;
					hingeLastAngles[num] = lastAngle;
					bones[num].SetLocalRotation(s, limitDefaultLocalRotationArray[num] * quaternion2);
				}
				else if (angleFlags[num] == 1)
				{
					Quaternion quaternion3 = RotationLimitUtilities.LimitAngle(Quaternion.Inverse(limitDefaultLocalRotationArray[num]) * bones[num].GetLocalRotation(s), limitAxisArray[num], angleSecondaryAxisArray[num], angleLimitArray[num].GetFloat(s), angleTwistLimitArray[num].GetFloat(s));
					bones[num].SetLocalRotation(s, limitDefaultLocalRotationArray[num] * quaternion3);
				}
			}
		}
	}

	private Vector3 GetLocalDirection(AnimationStream s)
	{
		return Quaternion.Inverse(bones[0].GetRotation(s)) * (bones[bones.Length - 1].GetPosition(s) - bones[0].GetPosition(s));
	}

	private float GetPositionOffset(AnimationStream s, Vector3 localDirection)
	{
		return Vector3.SqrMagnitude(localDirection - lastLocalDirection);
	}

	private Vector3 GetSingularityOffset(AnimationStream s, Vector3 IKPosition, bool useRotationLimits)
	{
		if (!SingularityDetected(s, IKPosition))
		{
			return Vector3.zero;
		}
		Vector3 normalized = (IKPosition - bones[0].GetPosition(s)).normalized;
		Vector3 rhs = new Vector3(normalized.y, normalized.z, normalized.x);
		if (useRotationLimits && hingeFlags[bones.Length - 2] == 1)
		{
			rhs = bones[bones.Length - 2].GetRotation(s) * limitAxisArray[bones.Length - 2];
		}
		return Vector3.Cross(normalized, rhs) * Mathf.Sqrt(boneSqrMags[bones.Length - 2]) * 0.5f;
	}

	private bool SingularityDetected(AnimationStream s, Vector3 IKPosition)
	{
		Vector3 position = bones[0].GetPosition(s);
		Vector3 lhs = bones[bones.Length - 1].GetPosition(s) - position;
		Vector3 rhs = IKPosition - position;
		float sqrMagnitude = lhs.sqrMagnitude;
		float sqrMagnitude2 = rhs.sqrMagnitude;
		if (sqrMagnitude < sqrMagnitude2)
		{
			return false;
		}
		if (sqrMagnitude < chainSqrMag - boneSqrMags[bones.Length - 2] * 0.1f)
		{
			return false;
		}
		if (sqrMagnitude == 0f)
		{
			return false;
		}
		if (sqrMagnitude2 == 0f)
		{
			return false;
		}
		if (Vector3.Dot(lhs, rhs) < 0.999f)
		{
			return false;
		}
		return true;
	}

	public void Dispose()
	{
		bones.Dispose();
		boneWeights.Dispose();
		boneSqrMags.Dispose();
		DisposeRotationLimits();
	}
}
