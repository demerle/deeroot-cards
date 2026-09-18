using UnityEngine;

namespace RootMotion.FinalIK;

public static class RotationLimitUtilities
{
	public static Quaternion LimitHinge(Quaternion rotation, float min, float max, bool useLimits, Vector3 axis, ref Quaternion lastRotation, ref float lastAngle)
	{
		if (min == 0f && max == 0f && useLimits)
		{
			return Quaternion.AngleAxis(0f, axis);
		}
		Quaternion quaternion = Limit1DOF(rotation, axis);
		if (!useLimits)
		{
			return quaternion;
		}
		Quaternion quaternion2 = quaternion * Quaternion.Inverse(lastRotation);
		float num = Quaternion.Angle(Quaternion.identity, quaternion2);
		Vector3 vector = new Vector3(axis.z, axis.x, axis.y);
		Vector3 rhs = Vector3.Cross(vector, axis);
		if (Vector3.Dot(quaternion2 * vector, rhs) > 0f)
		{
			num = 0f - num;
		}
		lastAngle = Mathf.Clamp(lastAngle + num, min, max);
		lastRotation = Quaternion.AngleAxis(lastAngle, axis);
		return lastRotation;
	}

	public static Quaternion Limit1DOF(Quaternion rotation, Vector3 axis)
	{
		return Quaternion.FromToRotation(rotation * axis, axis) * rotation;
	}

	public static Quaternion LimitAngle(Quaternion rotation, Vector3 axis, Vector3 secondaryAxis, float limit, float twistLimit)
	{
		return LimitTwist(LimitSwing(rotation, axis, limit), axis, secondaryAxis, twistLimit);
	}

	public static Quaternion LimitSwing(Quaternion rotation, Vector3 axis, float limit)
	{
		if (axis == Vector3.zero)
		{
			return rotation;
		}
		if (rotation == Quaternion.identity)
		{
			return rotation;
		}
		if (limit >= 180f)
		{
			return rotation;
		}
		Vector3 vector = rotation * axis;
		Quaternion to = Quaternion.FromToRotation(axis, vector);
		Quaternion quaternion = Quaternion.RotateTowards(Quaternion.identity, to, limit);
		return Quaternion.FromToRotation(vector, quaternion * axis) * rotation;
	}

	public static Quaternion LimitTwist(Quaternion rotation, Vector3 axis, Vector3 orthoAxis, float twistLimit)
	{
		twistLimit = Mathf.Clamp(twistLimit, 0f, 180f);
		if (twistLimit >= 180f)
		{
			return rotation;
		}
		Vector3 normal = rotation * axis;
		Vector3 tangent = orthoAxis;
		Vector3.OrthoNormalize(ref normal, ref tangent);
		Vector3 tangent2 = rotation * orthoAxis;
		Vector3.OrthoNormalize(ref normal, ref tangent2);
		Quaternion quaternion = Quaternion.FromToRotation(tangent2, tangent) * rotation;
		if (twistLimit <= 0f)
		{
			return quaternion;
		}
		return Quaternion.RotateTowards(quaternion, rotation, twistLimit);
	}
}
