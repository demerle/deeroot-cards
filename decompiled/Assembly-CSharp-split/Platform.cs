using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Platform
{
	private List<BoxCollider2D> m_boxColliders = new List<BoxCollider2D>();

	private List<Vector2> m_platformPoints = new List<Vector2>();

	public List<Vector2> m_edges = new List<Vector2>();

	private static float EPSILON = Mathf.Epsilon * 10f;

	private int m_lastCalculatedClosestPoint = -1;

	public List<BoxCollider2D> BoxColliders => m_boxColliders;

	public List<Vector2> PlatformPoints
	{
		get
		{
			return m_platformPoints;
		}
		set
		{
			m_platformPoints = value;
		}
	}

	public List<Vector2> Edges => m_edges;

	public Color Color { get; set; }

	public Platform()
	{
		Color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
	}

	public bool ContainsCollider(BoxCollider2D collider)
	{
		foreach (BoxCollider2D boxCollider in m_boxColliders)
		{
			if (collider == boxCollider)
			{
				return true;
			}
		}
		return false;
	}

	public void AddCollider(BoxCollider2D collider)
	{
		m_boxColliders.Add(collider);
	}

	public void AddPlatformPoint(Vector2 point)
	{
		m_platformPoints.Add(point);
	}

	public float GetClosestDistance(Vector2 position)
	{
		float num = float.MaxValue;
		for (int i = 0; i < m_edges.Count; i++)
		{
			float num2 = Vector2.Distance(position, m_edges[i]);
			if (num2 < num)
			{
				m_lastCalculatedClosestPoint = i;
				num = num2;
			}
		}
		return num;
	}

	public bool IsPositionOutsidePlatform(Vector2 position)
	{
		Vector2 vector = m_edges[m_lastCalculatedClosestPoint];
		if (position.x > vector.x && m_lastCalculatedClosestPoint == m_edges.Count - 1)
		{
			return true;
		}
		if (position.x < vector.x && m_lastCalculatedClosestPoint == 0)
		{
			return true;
		}
		return false;
	}

	public Vector2 GetPointOnPath(Vector2 position)
	{
		Vector2 vector = m_edges[m_lastCalculatedClosestPoint];
		if (position.x > vector.x && m_lastCalculatedClosestPoint == m_edges.Count - 1)
		{
			return vector;
		}
		if (position.x < vector.x && m_lastCalculatedClosestPoint == 0)
		{
			return vector;
		}
		int index = -1;
		int num = 0;
		if (position.x > vector.x)
		{
			index = m_lastCalculatedClosestPoint + 1;
			num = 1;
		}
		else if (position.x <= vector.x)
		{
			index = m_lastCalculatedClosestPoint - 1;
			num = -1;
		}
		if (num == 1)
		{
			return GetClosestPointOnLineSegment(vector, m_edges[index], position);
		}
		return GetClosestPointOnLineSegment(m_edges[index], vector, position);
	}

	private Vector2 Project(Vector2 line1, Vector2 line2, Vector2 toProject)
	{
		float num = (line2.y - line1.y) / (line2.x - line1.x);
		float num2 = line1.y - num * line1.x;
		float x = (num * toProject.y + toProject.x - num * num2) / (num * num + 1f);
		float y = (num * num * toProject.y + num * toProject.x + num2) / (num * num + 1f);
		return new Vector2(x, y);
	}

	public static Vector2 GetClosestPointOnLineSegment(Vector2 A, Vector2 B, Vector2 P)
	{
		Vector2 lhs = P - A;
		Vector2 vector = B - A;
		float sqrMagnitude = vector.sqrMagnitude;
		float num = Vector2.Dot(lhs, vector) / sqrMagnitude;
		if (num < 0f)
		{
			return A;
		}
		if (num > 1f)
		{
			return B;
		}
		return A + vector * num;
	}

	public void PostProcessPlatformPoints()
	{
		int mask = LayerMask.GetMask("Default");
		List<Vector2> list = new List<Vector2>(m_platformPoints.OrderBy((Vector2 v) => v.x).ToArray());
		new HashSet<int>();
		for (int num = list.Count - 1; num > 0; num--)
		{
			if (Physics2D.OverlapCircle(list[num] + new Vector2(0f, 0.25f), 0.2f, mask) != null)
			{
				list.RemoveAt(num);
			}
		}
		m_platformPoints = list;
		DetectEdges();
	}

	public void DetectEdges()
	{
		if (m_platformPoints.Count == 0)
		{
			return;
		}
		List<Vector2> list = new List<Vector2>();
		for (int i = 0; i < m_platformPoints.Count - 2; i++)
		{
			Vector2 item = m_platformPoints[i];
			Vector2 item2 = m_platformPoints[i + 1];
			if (i == 0)
			{
				list.Add(item);
			}
			else if (i == m_platformPoints.Count - 3)
			{
				list.Add(item2);
			}
			else if (item2.y - item.y > Mathf.Epsilon)
			{
				list.Add(item);
				list.Add(item2);
			}
		}
		m_edges = list;
	}

	public void DrawGizmos()
	{
		Gizmos.color = Color;
		for (int i = 0; i < m_edges.Count; i++)
		{
			Gizmos.DrawSphere(m_edges[i], 0.2f);
		}
		for (int j = 0; j < m_edges.Count - 1; j++)
		{
			Gizmos.DrawLine(m_edges[j], m_edges[j + 1]);
		}
	}
}
