using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Animations;

namespace RootMotion.FinalIK;

public struct AimIKJob : IAnimationJob
{
	public TransformSceneHandle _target;

	public TransformSceneHandle _poleTarget;

	public TransformStreamHandle _transform;

	public PropertySceneHandle _IKPositionWeight;

	public PropertySceneHandle _poleWeight;

	public PropertySceneHandle _axisX;

	public PropertySceneHandle _axisY;

	public PropertySceneHandle _axisZ;

	public PropertySceneHandle _poleAxisX;

	public PropertySceneHandle _poleAxisY;

	public PropertySceneHandle _poleAxisZ;

	public PropertySceneHandle _clampWeight;

	public PropertySceneHandle _clampSmoothing;

	public PropertySceneHandle _maxIterations;

	public PropertySceneHandle _tolerance;

	public PropertySceneHandle _XY;

	public PropertySceneHandle _useRotationLimits;

	private NativeArray<TransformStreamHandle> bones;

	private NativeArray<PropertySceneHandle> boneWeights;

	private Vector3 lastLocalDirection;

	private float step;

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

	public void Setup(Animator animator, Transform[] bones, Transform target, Transform poleTarget, Transform aimTransform)
	{
		this.bones = new NativeArray<TransformStreamHandle>(bones.Length, Allocator.Persistent);
		boneWeights = new NativeArray<PropertySceneHandle>(bones.Length, Allocator.Persistent);
		for (int i = 0; i < this.bones.Length; i++)
		{
			this.bones[i] = animator.BindStreamTransform(bones[i]);
		}
		for (int j = 0; j < this.bones.Length; j++)
		{
			if (bones[j].gameObject.GetComponent<IKJBoneParams>() == null)
			{
				bones[j].gameObject.AddComponent<IKJBoneParams>();
			}
			boneWeights[j] = animator.BindSceneProperty(bones[j].transform, typeof(IKJBoneParams), "weight");
		}
		SetUpRotationLimits(animator, bones);
		_target = animator.BindSceneTransform(target);
		_poleTarget = animator.BindSceneTransform(poleTarget);
		_transform = animator.BindStreamTransform(aimTransform);
		_IKPositionWeight = animator.BindSceneProperty(animator.transform, typeof(AimIKJ), "weight");
		_poleWeight = animator.BindSceneProperty(animator.transform, typeof(AimIKJ), "poleWeight");
		_axisX = animator.BindSceneProperty(animator.transform, typeof(AimIKJ), "axisX");
		_axisY = animator.BindSceneProperty(animator.transform, typeof(AimIKJ), "axisY");
		_axisZ = animator.BindSceneProperty(animator.transform, typeof(AimIKJ), "axisZ");
		_poleAxisX = animator.BindSceneProperty(animator.transform, typeof(AimIKJ), "poleAxisX");
		_poleAxisY = animator.BindSceneProperty(animator.transform, typeof(AimIKJ), "poleAxisY");
		_poleAxisZ = animator.BindSceneProperty(animator.transform, typeof(AimIKJ), "poleAxisZ");
		_clampWeight = animator.BindSceneProperty(animator.transform, typeof(AimIKJ), "clampWeight");
		_clampSmoothing = animator.BindSceneProperty(animator.transform, typeof(AimIKJ), "clampSmoothing");
		_maxIterations = animator.BindSceneProperty(animator.transform, typeof(AimIKJ), "maxIterations");
		_tolerance = animator.BindSceneProperty(animator.transform, typeof(AimIKJ), "tolerance");
		_XY = animator.BindSceneProperty(animator.transform, typeof(AimIKJ), "XY");
		_useRotationLimits = animator.BindSceneProperty(animator.transform, typeof(AimIKJ), "useRotationLimits");
		step = 1f / (float)bones.Length;
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
		if (!_target.IsValid(s) || !_poleTarget.IsValid(s) || !_transform.IsValid(s))
		{
			return;
		}
		Vector3 vector = new Vector3(_axisX.GetFloat(s), _axisY.GetFloat(s), _axisZ.GetFloat(s));
		Vector3 vector2 = new Vector3(_poleAxisX.GetFloat(s), _poleAxisY.GetFloat(s), _poleAxisZ.GetFloat(s));
		float value = _poleWeight.GetFloat(s);
		value = Mathf.Clamp(value, 0f, 1f);
		if (vector == Vector3.zero || (vector2 == Vector3.zero && value > 0f))
		{
			return;
		}
		float num = _IKPositionWeight.GetFloat(s);
		if (num <= 0f)
		{
			return;
		}
		num = Mathf.Min(num, 1f);
		bool flag = _XY.GetBool(s);
		float num2 = _maxIterations.GetInt(s);
		float num3 = _tolerance.GetFloat(s);
		bool useRotationLimits = _useRotationLimits.GetBool(s);
		Vector3 position = _target.GetPosition(s);
		if (flag)
		{
			position.z = bones[0].GetPosition(s).z;
		}
		Vector3 position2 = _poleTarget.GetPosition(s);
		if (flag)
		{
			position2.z = position.z;
		}
		float value2 = _clampWeight.GetFloat(s);
		value2 = Mathf.Clamp(value2, 0f, 1f);
		int value3 = _clampSmoothing.GetInt(s);
		value3 = Mathf.Clamp(value3, 0, 2);
		Vector3 position3 = _transform.GetPosition(s);
		Vector3 vector3 = _transform.GetRotation(s) * vector;
		Vector3 clampedIKPosition = GetClampedIKPosition(s, value2, value3, position, position3, vector3);
		Vector3 b = clampedIKPosition - position3;
		b = Vector3.Slerp(vector3 * b.magnitude, b, num);
		clampedIKPosition = position3 + b;
		for (int i = 0; (float)i < num2 && (i < 0 || !(num3 > 0f) || !(GetAngle(s, vector, position) < num3)); i++)
		{
			lastLocalDirection = GetLocalDirection(s, _transform.GetRotation(s) * vector);
			for (int j = 0; j < bones.Length - 1; j++)
			{
				RotateToTarget(s, clampedIKPosition, position2, j, step * (float)(j + 1) * num * boneWeights[j].GetFloat(s), value, flag, useRotationLimits, vector, vector2);
			}
			RotateToTarget(s, clampedIKPosition, position2, bones.Length - 1, num * boneWeights[bones.Length - 1].GetFloat(s), value, flag, useRotationLimits, vector, vector2);
		}
		lastLocalDirection = GetLocalDirection(s, _transform.GetRotation(s) * vector);
	}

	private void RotateToTarget(AnimationStream s, Vector3 targetPosition, Vector3 polePosition, int i, float weight, float poleWeight, bool XY, bool useRotationLimits, Vector3 axis, Vector3 poleAxis)
	{
		if (XY)
		{
			if (weight >= 0f)
			{
				Vector3 vector = _transform.GetRotation(s) * axis;
				Vector3 vector2 = targetPosition - _transform.GetPosition(s);
				float current = Mathf.Atan2(vector.x, vector.y) * 57.29578f;
				float target = Mathf.Atan2(vector2.x, vector2.y) * 57.29578f;
				bones[i].SetRotation(s, Quaternion.AngleAxis(Mathf.DeltaAngle(current, target), Vector3.back) * bones[i].GetRotation(s));
			}
		}
		else
		{
			if (weight >= 0f)
			{
				Quaternion quaternion = Quaternion.FromToRotation(_transform.GetRotation(s) * axis, targetPosition - _transform.GetPosition(s));
				if (weight >= 1f)
				{
					bones[i].SetRotation(s, quaternion * bones[i].GetRotation(s));
				}
				else
				{
					bones[i].SetRotation(s, Quaternion.Lerp(Quaternion.identity, quaternion, weight) * bones[i].GetRotation(s));
				}
			}
			if (poleWeight > 0f)
			{
				Vector3 tangent = polePosition - _transform.GetPosition(s);
				Vector3 normal = _transform.GetRotation(s) * axis;
				Vector3.OrthoNormalize(ref normal, ref tangent);
				Quaternion b = Quaternion.FromToRotation(_transform.GetRotation(s) * poleAxis, tangent);
				bones[i].SetRotation(s, Quaternion.Lerp(Quaternion.identity, b, weight * poleWeight) * bones[i].GetRotation(s));
			}
		}
		if (useRotationLimits)
		{
			if (hingeFlags[i] == 1)
			{
				Quaternion rotation = Quaternion.Inverse(limitDefaultLocalRotationArray[i]) * bones[i].GetLocalRotation(s);
				Quaternion lastRotation = hingeLastRotations[i];
				float lastAngle = hingeLastAngles[i];
				Quaternion quaternion2 = RotationLimitUtilities.LimitHinge(rotation, hingeMinArray[i].GetFloat(s), hingeMaxArray[i].GetFloat(s), hingeUseLimitsArray[i].GetBool(s), limitAxisArray[i], ref lastRotation, ref lastAngle);
				hingeLastRotations[i] = lastRotation;
				hingeLastAngles[i] = lastAngle;
				bones[i].SetLocalRotation(s, limitDefaultLocalRotationArray[i] * quaternion2);
			}
			else if (angleFlags[i] == 1)
			{
				Quaternion quaternion3 = RotationLimitUtilities.LimitAngle(Quaternion.Inverse(limitDefaultLocalRotationArray[i]) * bones[i].GetLocalRotation(s), limitAxisArray[i], angleSecondaryAxisArray[i], angleLimitArray[i].GetFloat(s), angleTwistLimitArray[i].GetFloat(s));
				bones[i].SetLocalRotation(s, limitDefaultLocalRotationArray[i] * quaternion3);
			}
		}
	}

	public float GetAngle(AnimationStream s, Vector3 axis, Vector3 IKPosition)
	{
		return Vector3.Angle(_transform.GetRotation(s) * axis, IKPosition - _transform.GetPosition(s));
	}

	private Vector3 GetClampedIKPosition(AnimationStream s, float clampWeight, int clampSmoothing, Vector3 IKPosition, Vector3 transformPosition, Vector3 transformAxis)
	{
		if (clampWeight <= 0f)
		{
			return IKPosition;
		}
		if (clampWeight >= 1f)
		{
			return transformPosition + transformAxis * (IKPosition - transformPosition).magnitude;
		}
		float num = Vector3.Angle(transformAxis, IKPosition - transformPosition);
		float num2 = 1f - num / 180f;
		float num3 = ((clampWeight > 0f) ? Mathf.Clamp(1f - (clampWeight - num2) / (1f - num2), 0f, 1f) : 1f);
		float num4 = ((clampWeight > 0f) ? Mathf.Clamp(num2 / clampWeight, 0f, 1f) : 1f);
		for (int i = 0; i < clampSmoothing; i++)
		{
			num4 = Mathf.Sin(num4 * (float)Math.PI * 0.5f);
		}
		return transformPosition + Vector3.Slerp(transformAxis * 10f, IKPosition - transformPosition, num4 * num3);
	}

	private Vector3 GetLocalDirection(AnimationStream s, Vector3 transformAxis)
	{
		return Quaternion.Inverse(bones[0].GetRotation(s)) * transformAxis;
	}

	private float GetPositionOffset(AnimationStream s, Vector3 localDirection)
	{
		return Vector3.SqrMagnitude(localDirection - lastLocalDirection);
	}

	public void Dispose()
	{
		bones.Dispose();
		boneWeights.Dispose();
		DisposeRotationLimits();
	}
}
