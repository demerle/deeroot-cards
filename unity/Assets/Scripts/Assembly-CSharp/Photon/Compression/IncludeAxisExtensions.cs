using UnityEngine;

namespace Photon.Compression
{
	public static class IncludeAxisExtensions
	{
		public static float SqrMagnitude(this Vector3 v, IncludedAxes ia)
		{
			return (((ia & IncludedAxes.X) != 0) ? (v.x * v.x) : 0f) + (((ia & IncludedAxes.Y) != 0) ? (v.y * v.y) : 0f) + (((ia & IncludedAxes.Z) != 0) ? (v.z * v.z) : 0f);
		}

		public static float Magnitude(this Vector3 v, IncludedAxes ia)
		{
			return Mathf.Sqrt((((ia & IncludedAxes.X) != 0) ? (v.x * v.x) : 0f) + (((ia & IncludedAxes.Y) != 0) ? (v.y * v.y) : 0f) + (((ia & IncludedAxes.Z) != 0) ? (v.z * v.z) : 0f));
		}

		public static Vector3 Lerp(this GameObject go, Vector3 start, Vector3 end, IncludedAxes ia, float t, bool localPosition = false)
		{
			Vector3 vector = Vector3.Lerp(start, end, t);
			return new Vector3(((ia & IncludedAxes.X) != 0) ? vector[0] : (localPosition ? go.transform.localPosition[0] : go.transform.position[0]), ((ia & IncludedAxes.Y) != 0) ? vector[1] : (localPosition ? go.transform.localPosition[1] : go.transform.position[1]), ((ia & IncludedAxes.Z) != 0) ? vector[2] : (localPosition ? go.transform.localPosition[2] : go.transform.position[2]));
		}

		public static void SetPosition(this GameObject go, Vector3 pos, IncludedAxes ia, bool localPosition = false)
		{
			Vector3 vector = new Vector3(((ia & IncludedAxes.X) != 0) ? pos[0] : (localPosition ? go.transform.localPosition[0] : go.transform.position[0]), ((ia & IncludedAxes.Y) != 0) ? pos[1] : (localPosition ? go.transform.localPosition[1] : go.transform.position[1]), ((ia & IncludedAxes.Z) != 0) ? pos[2] : (localPosition ? go.transform.localPosition[2] : go.transform.position[2]));
			if (!localPosition)
			{
				go.transform.position = vector;
			}
			else
			{
				go.transform.localPosition = vector;
			}
		}
	}
}
