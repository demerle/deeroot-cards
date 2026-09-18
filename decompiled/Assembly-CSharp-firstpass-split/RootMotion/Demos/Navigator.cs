using System;
using UnityEngine;
using UnityEngine.AI;

namespace RootMotion.Demos;

[Serializable]
public class Navigator
{
	public enum State
	{
		Idle,
		Seeking,
		OnPath
	}

	[Tooltip("Should this Navigator be actively seeking a path.")]
	public bool activeTargetSeeking;

	[Tooltip("Increase this value if the character starts running in a circle, not able to reach the corner because of a too large turning radius.")]
	public float cornerRadius = 0.5f;

	[Tooltip("Recalculate path if target position has moved by this distance from the position it was at when the path was originally calculated")]
	public float recalculateOnPathDistance = 1f;

	[Tooltip("Sample within this distance from sourcePosition.")]
	public float maxSampleDistance = 5f;

	[Tooltip("Interval of updating the path")]
	public float nextPathInterval = 3f;

	private Transform transform;

	private int cornerIndex;

	private Vector3[] corners = new Vector3[0];

	private NavMeshPath path;

	private Vector3 lastTargetPosition;

	private bool initiated;

	private float nextPathTime;

	public Vector3 normalizedDeltaPosition { get; private set; }

	public State state { get; private set; }

	public void Initiate(Transform transform)
	{
		this.transform = transform;
		path = new NavMeshPath();
		initiated = true;
		cornerIndex = 0;
		corners = new Vector3[0];
		state = State.Idle;
		lastTargetPosition = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
	}

	public void Update(Vector3 targetPosition)
	{
		if (!initiated)
		{
			Debug.LogError("Trying to update an uninitiated Navigator.");
			return;
		}
		switch (state)
		{
		case State.Seeking:
			normalizedDeltaPosition = Vector3.zero;
			if (path.status == NavMeshPathStatus.PathComplete)
			{
				corners = path.corners;
				cornerIndex = 0;
				if (corners.Length == 0)
				{
					Debug.LogWarning("Zero Corner Path", transform);
					Stop();
				}
				else
				{
					state = State.OnPath;
				}
			}
			if (path.status == NavMeshPathStatus.PathPartial)
			{
				Debug.LogWarning("Path Partial", transform);
			}
			if (path.status == NavMeshPathStatus.PathInvalid)
			{
				Debug.LogWarning("Path Invalid", transform);
			}
			break;
		case State.OnPath:
			if (activeTargetSeeking && Time.time > nextPathTime && HorDistance(targetPosition, lastTargetPosition) > recalculateOnPathDistance)
			{
				CalculatePath(targetPosition);
			}
			else
			{
				if (cornerIndex >= corners.Length)
				{
					break;
				}
				Vector3 vector = corners[cornerIndex] - transform.position;
				vector.y = 0f;
				float magnitude = vector.magnitude;
				if (magnitude > 0f)
				{
					normalizedDeltaPosition = vector / vector.magnitude;
				}
				else
				{
					normalizedDeltaPosition = Vector3.zero;
				}
				if (magnitude < cornerRadius)
				{
					cornerIndex++;
					if (cornerIndex >= corners.Length)
					{
						Stop();
					}
				}
			}
			break;
		case State.Idle:
			if (activeTargetSeeking && Time.time > nextPathTime)
			{
				CalculatePath(targetPosition);
			}
			break;
		}
	}

	private void CalculatePath(Vector3 targetPosition)
	{
		if (Find(targetPosition))
		{
			lastTargetPosition = targetPosition;
			state = State.Seeking;
		}
		else
		{
			Stop();
		}
		nextPathTime = Time.time + nextPathInterval;
	}

	private bool Find(Vector3 targetPosition)
	{
		if (HorDistance(transform.position, targetPosition) < cornerRadius * 2f)
		{
			return false;
		}
		if (NavMesh.CalculatePath(transform.position, targetPosition, -1, path))
		{
			return true;
		}
		NavMeshHit hit = default(NavMeshHit);
		if (NavMesh.SamplePosition(targetPosition, out hit, maxSampleDistance, -1) && NavMesh.CalculatePath(transform.position, hit.position, -1, path))
		{
			return true;
		}
		return false;
	}

	private void Stop()
	{
		state = State.Idle;
		normalizedDeltaPosition = Vector3.zero;
	}

	private float HorDistance(Vector3 p1, Vector3 p2)
	{
		return Vector2.Distance(new Vector2(p1.x, p1.z), new Vector2(p2.x, p2.z));
	}

	public void Visualize()
	{
		if (state == State.Idle)
		{
			Gizmos.color = Color.gray;
		}
		if (state == State.Seeking)
		{
			Gizmos.color = Color.red;
		}
		if (state == State.OnPath)
		{
			Gizmos.color = Color.green;
		}
		if (corners.Length != 0 && state == State.OnPath && cornerIndex == 0)
		{
			Gizmos.DrawLine(transform.position, corners[0]);
		}
		for (int i = 0; i < corners.Length; i++)
		{
			Gizmos.DrawSphere(corners[i], 0.1f);
		}
		if (corners.Length > 1)
		{
			for (int j = 0; j < corners.Length - 1; j++)
			{
				Gizmos.DrawLine(corners[j], corners[j + 1]);
			}
		}
		Gizmos.color = Color.white;
	}
}
