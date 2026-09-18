using UnityEngine;

namespace Photon.Compression;

public static class IncludeAxisExtensions
{
	public static float SqrMagnitude(this Vector3 v, IncludedAxes ia)
	{
		return (((ia & IncludedAxes.X) != IncludedAxes.None) ? (v.x * v.x) : 0f) + (((ia & IncludedAxes.Y) != IncludedAxes.None) ? (v.y * v.y) : 0f) + (((ia & IncludedAxes.Z) != IncludedAxes.None) ? (v.z * v.z) : 0f);
	}

	public static float Magnitude(this Vector3 v, IncludedAxes ia)
	{
		return Mathf.Sqrt((((ia & IncludedAxes.X) != IncludedAxes.None) ? (v.x * v.x) : 0f) + (((ia & IncludedAxes.Y) != IncludedAxes.None) ? (v.y * v.y) : 0f) + (((ia & IncludedAxes.Z) != IncludedAxes.None) ? (v.z * v.z) : 0f));
	}

	public static Vector3 Lerp(this GameObject go, Vector3 start, Vector3 end, IncludedAxes ia, float t, bool localPosition = false)
	{
		Vector3 vector = Vector3.Lerp(start, end, t);
		return new Vector3(((ia & IncludedAxes.X) != IncludedAxes.None) ? vector[0] : (localPosition ? go.transform.localPosition[0] : go.transform.position[0]), ((ia & IncludedAxes.Y) != IncludedAxes.None) ? vector[1] : (localPosition ? go.transform.localPosition[1] : go.transform.position[1]), ((ia & IncludedAxes.Z) != IncludedAxes.None) ? vector[2] : (localPosition ? go.transform.localPosition[2] : go.transform.position[2]));
	}

	public static void SetPosition(this GameObject go, Vector3 pos, IncludedAxes ia, bool localPosition = false)
	{
		Vector3 vector = new Vector3(((ia & IncludedAxes.X) != IncludedAxes.None) ? pos[0] : (localPosition ? go.transform.localPosition[0] : go.transform.position[0]), ((ia & IncludedAxes.Y) != IncludedAxes.None) ? pos[1] : (localPosition ? go.transform.localPosition[1] : go.transform.position[1]), ((ia & IncludedAxes.Z) != IncludedAxes.None) ? pos[2] : (localPosition ? go.transform.localPosition[2] : go.transform.position[2]));
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
