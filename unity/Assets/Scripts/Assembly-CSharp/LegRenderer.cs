using System.Collections.Generic;
using UnityEngine;

public class LegRenderer : MonoBehaviour
{
	public Transform start;

	public Transform mid;

	public Transform end;

	public int segmentCount = 10;

	public GameObject segment;

	public float segmentLength = 1f;

	private List<Transform> segments = new List<Transform>();

	private void Awake()
	{
		for (int i = 0; i < segmentCount; i++)
		{
			GameObject gameObject = Object.Instantiate(segment, base.transform);
			gameObject.SetActive(true);
			segments.Add(gameObject.transform);
			if (i == segmentCount - 1)
			{
				gameObject.transform.localScale *= 0f;
			}
		}
	}

	private void LateUpdate()
	{
		for (int i = 0; i < segments.Count; i++)
		{
			float t = (float)i / ((float)segments.Count - 1f);
			Vector3 zero2 = Vector3.zero;
			if (i == 0)
			{
				Vector3 vector5 = segments[i + 1].position - segments[i].position;
			}
			else if (i == segments.Count - 1)
			{
				Vector3 vector6 = segments[i].position - segments[i - 1].position;
			}
			else
			{
				Vector3 vector = segments[i].position - segments[i - 1].position;
				Vector3 vector2 = segments[i + 1].position - segments[i].position;
				Vector3 vector7 = (vector + vector2) * 0.5f;
			}
			segments[i].position = BezierCurve.QuadraticBezier(start.position, mid.position, end.position, t);
		}
		for (int j = 0; j < segments.Count; j++)
		{
			float num = (float)j / ((float)segments.Count - 1f);
			Vector3 zero = Vector3.zero;
			if (j == 0)
			{
				zero = segments[j + 1].position - segments[j].position;
			}
			else if (j == segments.Count - 1)
			{
				zero = segments[j].position - segments[j - 1].position;
			}
			else
			{
				Vector3 vector3 = segments[j].position - segments[j - 1].position;
				Vector3 vector4 = segments[j + 1].position - segments[j].position;
				zero = (vector3 + vector4) * 0.5f;
			}
			segments[j].rotation = Quaternion.LookRotation(Vector3.forward, zero);
		}
	}
}
