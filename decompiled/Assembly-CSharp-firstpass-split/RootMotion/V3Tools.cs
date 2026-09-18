using System;
using UnityEngine;

namespace RootMotion;

public static class V3Tools
{
	public static float GetYaw(Vector3 forward)
	{
		return Mathf.Atan2(forward.x, forward.z) * 57.29578f;
	}

	public static float GetPitch(Vector3 forward)
	{
		forward = forward.normalized;
		return (0f - Mathf.Asin(forward.y)) * 57.29578f;
	}

	public static float GetBank(Vector3 forward, Vector3 up)
	{
		up = Quaternion.Inverse(Quaternion.LookRotation(Vector3.up, forward)) * up;
		return Mathf.Atan2(up.x, up.z) * 57.29578f;
	}

	public static float GetYaw(Vector3 spaceForward, Vector3 spaceUp, Vector3 forward)
	{
		Vector3 vector = Quaternion.Inverse(Quaternion.LookRotation(spaceForward, spaceUp)) * forward;
		return Mathf.Atan2(vector.x, vector.z) * 57.29578f;
	}

	public static float GetPitch(Vector3 spaceForward, Vector3 spaceUp, Vector3 forward)
	{
		return (0f - Mathf.Asin((Quaternion.Inverse(Quaternion.LookRotation(spaceForward, spaceUp)) * forward).y)) * 57.29578f;
	}

	public static float GetBank(Vector3 spaceForward, Vector3 spaceUp, Vector3 forward, Vector3 up)
	{
		Quaternion quaternion = Quaternion.Inverse(Quaternion.LookRotation(spaceForward, spaceUp));
		forward = quaternion * forward;
		up = quaternion * up;
		up = Quaternion.Inverse(Quaternion.LookRotation(spaceUp, forward)) * up;
		return Mathf.Atan2(up.x, up.z) * 57.29578f;
	}

	public static Vector3 Lerp(Vector3 fromVector, Vector3 toVector, float weight)
	{
		if (weight <= 0f)
		{
			return fromVector;
		}
		if (weight >= 1f)
		{
			return toVector;
		}
		return Vector3.Lerp(fromVector, toVector, weight);
	}

	public static Vector3 Slerp(Vector3 fromVector, Vector3 toVector, float weight)
	{
		if (weight <= 0f)
		{
			return fromVector;
		}
		if (weight >= 1f)
		{
			return toVector;
		}
		return Vector3.Slerp(fromVector, toVector, weight);
	}

	public static Vector3 ExtractVertical(Vector3 v, Vector3 verticalAxis, float weight)
	{
		if (weight == 0f)
		{
			return Vector3.zero;
		}
		return Vector3.Project(v, verticalAxis) * weight;
	}

	public static Vector3 ExtractHorizontal(Vector3 v, Vector3 normal, float weight)
	{
		if (weight == 0f)
		{
			return Vector3.zero;
		}
		Vector3 tangent = v;
		Vector3.OrthoNormalize(ref normal, ref tangent);
		return Vector3.Project(v, tangent) * weight;
	}

	public static Vector3 ClampDirection(Vector3 direction, Vector3 normalDirection, float clampWeight, int clampSmoothing)
	{
		if (clampWeight <= 0f)
		{
			return direction;
		}
		if (clampWeight >= 1f)
		{
			return normalDirection;
		}
		float num = Vector3.Angle(normalDirection, direction);
		float num2 = 1f - num / 180f;
		if (num2 > clampWeight)
		{
			return direction;
		}
		float num3 = ((clampWeight > 0f) ? Mathf.Clamp(1f - (clampWeight - num2) / (1f - num2), 0f, 1f) : 1f);
		float num4 = ((clampWeight > 0f) ? Mathf.Clamp(num2 / clampWeight, 0f, 1f) : 1f);
		for (int i = 0; i < clampSmoothing; i++)
		{
			num4 = Mathf.Sin(num4 * (float)Math.PI * 0.5f);
		}
		return Vector3.Slerp(normalDirection, direction, num4 * num3);
	}

	public static Vector3 ClampDirection(Vector3 direction, Vector3 normalDirection, float clampWeight, int clampSmoothing, out bool changed)
	{
		changed = false;
		if (clampWeight <= 0f)
		{
			return direction;
		}
		if (clampWeight >= 1f)
		{
			changed = true;
			return normalDirection;
		}
		float num = Vector3.Angle(normalDirection, direction);
		float num2 = 1f - num / 180f;
		if (num2 > clampWeight)
		{
			return direction;
		}
		changed = true;
		float num3 = ((clampWeight > 0f) ? Mathf.Clamp(1f - (clampWeight - num2) / (1f - num2), 0f, 1f) : 1f);
		float num4 = ((clampWeight > 0f) ? Mathf.Clamp(num2 / clampWeight, 0f, 1f) : 1f);
		for (int i = 0; i < clampSmoothing; i++)
		{
			num4 = Mathf.Sin(num4 * (float)Math.PI * 0.5f);
		}
		return Vector3.Slerp(normalDirection, direction, num4 * num3);
	}

	public static Vector3 ClampDirection(Vector3 direction, Vector3 normalDirection, float clampWeight, int clampSmoothing, out float clampValue)
	{
		clampValue = 1f;
		if (clampWeight <= 0f)
		{
			return direction;
		}
		if (clampWeight >= 1f)
		{
			return normalDirection;
		}
		float num = Vector3.Angle(normalDirection, direction);
		float num2 = 1f - num / 180f;
		if (num2 > clampWeight)
		{
			clampValue = 0f;
			return direction;
		}
		float num3 = ((clampWeight > 0f) ? Mathf.Clamp(1f - (clampWeight - num2) / (1f - num2), 0f, 1f) : 1f);
		float num4 = ((clampWeight > 0f) ? Mathf.Clamp(num2 / clampWeight, 0f, 1f) : 1f);
		for (int i = 0; i < clampSmoothing; i++)
		{
			num4 = Mathf.Sin(num4 * (float)Math.PI * 0.5f);
		}
		float num5 = num4 * num3;
		clampValue = 1f - num5;
		return Vector3.Slerp(normalDirection, direction, num5);
	}

	public static Vector3 LineToPlane(Vector3 origin, Vector3 direction, Vector3 planeNormal, Vector3 planePoint)
	{
		float num = Vector3.Dot(planePoint - origin, planeNormal);
		float num2 = Vector3.Dot(direction, planeNormal);
		if (num2 == 0f)
		{
			return Vector3.zero;
		}
		float num3 = num / num2;
		return origin + direction.normalized * num3;
	}

	public static Vector3 PointToPlane(Vector3 point, Vector3 planePosition, Vector3 planeNormal)
	{
		if (planeNormal == Vector3.up)
		{
			return new Vector3(point.x, planePosition.y, point.z);
		}
		Vector3 tangent = point - planePosition;
		Vector3 normal = planeNormal;
		Vector3.OrthoNormalize(ref normal, ref tangent);
		return planePosition + Vector3.Project(point - planePosition, tangent);
	}

	public static Vector3 TransformPointUnscaled(Transform t, Vector3 point)
	{
		return t.position + t.rotation * point;
	}

	public static Vector3 InverseTransformPointUnscaled(Transform t, Vector3 point)
	{
		return Quaternion.Inverse(t.rotation) * (point - t.position);
	}

	public static Vector3 InverseTransformPoint(Vector3 tPos, Quaternion tRot, Vector3 tScale, Vector3 point)
	{
		return Div(Quaternion.Inverse(tRot) * (point - tPos), tScale);
	}

	public static Vector3 TransformPoint(Vector3 tPos, Quaternion tRot, Vector3 tScale, Vector3 point)
	{
		return tPos + Vector3.Scale(tRot * point, tScale);
	}

	public static Vector3 Div(Vector3 v1, Vector3 v2)
	{
		return new Vector3(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z);
	}
}
