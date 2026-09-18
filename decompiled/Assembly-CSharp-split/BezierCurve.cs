using UnityEngine;

public static class BezierCurve
{
	public static Vector3 CubicBezier(Vector3 Start, Vector3 _P1, Vector3 _P2, Vector3 end, float _t)
	{
		return (1f - _t) * QuadraticBezier(Start, _P1, _P2, _t) + _t * QuadraticBezier(_P1, _P2, end, _t);
	}

	public static Vector3 QuadraticBezier(Vector3 start, Vector3 _P1, Vector3 end, float _t)
	{
		return (1f - _t) * LinearBezier(start, _P1, _t) + _t * LinearBezier(_P1, end, _t);
	}

	public static Vector3 LinearBezier(Vector3 start, Vector3 end, float _t)
	{
		return (1f - _t) * start + _t * end;
	}
}
