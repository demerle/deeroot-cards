using UnityEngine;

namespace RootMotion;

public static class BakerUtilities
{
	public static void ReduceKeyframes(AnimationCurve curve, float maxError)
	{
		if (!(maxError <= 0f))
		{
			curve.keys = GetReducedKeyframes(curve, maxError);
		}
	}

	public static Keyframe[] GetReducedKeyframes(AnimationCurve curve, float maxError)
	{
		Keyframe[] array = curve.keys;
		int num = 1;
		while (num < array.Length - 1 && array.Length > 2)
		{
			Keyframe[] array2 = new Keyframe[array.Length - 1];
			int num2 = 0;
			for (int i = 0; i < array.Length; i++)
			{
				if (num != i)
				{
					array2[num2] = new Keyframe(array[i].time, array[i].value, array[i].inTangent, array[i].outTangent);
					num2++;
				}
			}
			AnimationCurve obj = new AnimationCurve
			{
				keys = array2
			};
			float num3 = Mathf.Abs(obj.Evaluate(array[num].time) - array[num].value);
			float time = array[num].time + (array[num - 1].time - array[num].time) * 0.5f;
			float time2 = array[num].time + (array[num + 1].time - array[num].time) * 0.5f;
			float num4 = Mathf.Abs(obj.Evaluate(time) - curve.Evaluate(time));
			float num5 = Mathf.Abs(obj.Evaluate(time2) - curve.Evaluate(time2));
			if (num3 < maxError && num4 < maxError && num5 < maxError)
			{
				array = array2;
			}
			else
			{
				num++;
			}
		}
		return array;
	}

	public static void SetLoopFrame(float time, AnimationCurve curve)
	{
		Keyframe[] keys = curve.keys;
		keys[^1].value = keys[0].value;
		float inTangent = Mathf.Lerp(keys[0].inTangent, keys[^1].inTangent, 0.5f);
		keys[0].inTangent = inTangent;
		keys[^1].inTangent = inTangent;
		float outTangent = Mathf.Lerp(keys[0].outTangent, keys[^1].outTangent, 0.5f);
		keys[0].outTangent = outTangent;
		keys[^1].outTangent = outTangent;
		keys[^1].time = time;
		curve.keys = keys;
	}

	public static void SetTangentMode(AnimationCurve curve)
	{
	}

	public static Quaternion EnsureQuaternionContinuity(Quaternion lastQ, Quaternion q)
	{
		Quaternion result = new Quaternion(0f - q.x, 0f - q.y, 0f - q.z, 0f - q.w);
		Quaternion b = new Quaternion(Mathf.Lerp(lastQ.x, q.x, 0.5f), Mathf.Lerp(lastQ.y, q.y, 0.5f), Mathf.Lerp(lastQ.z, q.z, 0.5f), Mathf.Lerp(lastQ.w, q.w, 0.5f));
		Quaternion b2 = new Quaternion(Mathf.Lerp(lastQ.x, result.x, 0.5f), Mathf.Lerp(lastQ.y, result.y, 0.5f), Mathf.Lerp(lastQ.z, result.z, 0.5f), Mathf.Lerp(lastQ.w, result.w, 0.5f));
		float num = Quaternion.Angle(lastQ, b);
		if (!(Quaternion.Angle(lastQ, b2) < num))
		{
			return q;
		}
		return result;
	}
}
