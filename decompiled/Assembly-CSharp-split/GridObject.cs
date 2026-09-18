using UnityEngine;

public class GridObject : MonoBehaviour
{
	public virtual void BopCall(float power)
	{
	}

	public virtual void BulletCall(float distance)
	{
		distance = Mathf.Clamp(0f, 1f, distance);
		BopCall(1f - distance);
	}

	public virtual void OnPlayerKilled(float distance)
	{
	}

	public virtual void OnGameOverOver(float distance)
	{
	}

	public virtual void OnSetSize(float size)
	{
	}
}
