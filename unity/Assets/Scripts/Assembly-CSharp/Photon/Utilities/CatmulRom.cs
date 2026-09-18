using UnityEngine;

namespace Photon.Utilities
{
	public class CatmulRom
	{
		public static float CatmullRomLerp(float pre, float start, float end, float post, float t)
		{
			while (t > 1f)
			{
				pre = start;
				start = end;
				end = post;
				post = end + (end - start);
				t -= 1f;
			}
			float num = 2f * start;
			float num2 = end - pre;
			float num3 = 2f * pre - 5f * start + 4f * end - post;
			float num4 = 0f - pre + 3f * (start - end) + post;
			float num5 = t * t;
			return (num + num2 * t + num3 * num5 + num4 * num5 * t) * 0.5f;
		}

		public static float CatmullRomLerp(float pre, float start, float end, float t)
		{
			float num = end + (end - start);
			while (t > 1f)
			{
				pre = start;
				start = end;
				end = num;
				num = end + (end - start);
				t -= 1f;
			}
			float num2 = 2f * start;
			float num3 = end - pre;
			float num4 = 2f * pre - 5f * start + 4f * end - num;
			float num5 = 0f - pre + 3f * (start - end) + num;
			float num6 = t * t;
			return (num2 + num3 * t + num4 * num6 + num5 * num6 * t) * 0.5f;
		}

		public static Vector3 CatmullRomLerp(Vector2 pre, Vector2 start, Vector2 end, Vector2 post, float t)
		{
			while (t > 1f)
			{
				pre = start;
				start = end;
				end = post;
				post = end + (end - start);
				t -= 1f;
			}
			Vector2 vector = 2f * start;
			Vector2 vector2 = end - pre;
			Vector2 vector3 = 2f * pre - 5f * start + 4f * end - post;
			Vector2 vector4 = -pre + 3f * (start - end) + post;
			float num = t * t;
			return (vector + vector2 * t + vector3 * num + vector4 * num * t) * 0.5f;
		}

		public static Vector3 CatmullRomLerp(Vector2 pre, Vector2 start, Vector2 end, float t)
		{
			Vector2 vector = end + (end - start);
			while (t > 1f)
			{
				pre = start;
				start = end;
				end = vector;
				vector = end + (end - start);
				t -= 1f;
			}
			Vector2 vector2 = 2f * start;
			Vector2 vector3 = end - pre;
			Vector2 vector4 = 2f * pre - 5f * start + 4f * end - vector;
			Vector2 vector5 = -pre + 3f * (start - end) + vector;
			float num = t * t;
			return (vector2 + vector3 * t + vector4 * num + vector5 * num * t) * 0.5f;
		}

		public static Vector3 CatmullRomLerp(Vector3 pre, Vector3 start, Vector3 end, Vector3 post, float t)
		{
			while (t > 1f)
			{
				pre = start;
				start = end;
				end = post;
				post = end + (end - start);
				t -= 1f;
			}
			Vector3 vector = 2f * start;
			Vector3 vector2 = end - pre;
			Vector3 vector3 = 2f * pre - 5f * start + 4f * end - post;
			Vector3 vector4 = -pre + 3f * (start - end) + post;
			float num = t * t;
			return (vector + vector2 * t + vector3 * num + vector4 * num * t) * 0.5f;
		}

		public static Vector3 CatmullRomLerp(Vector3 pre, Vector3 start, Vector3 end, float t)
		{
			Vector3 vector = end + (end - start);
			while (t > 1f)
			{
				pre = start;
				start = end;
				end = vector;
				vector = end + (end - start);
				t -= 1f;
			}
			Vector3 vector2 = 2f * start;
			Vector3 vector3 = end - pre;
			Vector3 vector4 = 2f * pre - 5f * start + 4f * end - vector;
			Vector3 vector5 = -pre + 3f * (start - end) + vector;
			float num = t * t;
			return (vector2 + vector3 * t + vector4 * num + vector5 * num * t) * 0.5f;
		}
	}
}
