using System;
using System.Collections.Generic;
using UnityEngine;

public class BrodalAIController : MonoBehaviour
{
	private PlayerAPI m_playerAPI;

	private int m_sampleCount = 250;

	private float m_sampleSize;

	private LayerMask m_layerMask;

	private Dictionary<Guid, Platform> m_platforms = new Dictionary<Guid, Platform>();

	private Platform m_currentPlatform;

	private Vector2 m_pointOnLine;

	private bool inited;

	private void Start()
	{
		m_playerAPI = GetComponentInParent<PlayerAPI>();
		HealthHandler healthHandler = m_playerAPI.player.data.healthHandler;
		healthHandler.delayedReviveAction = (Action)Delegate.Combine(healthHandler.delayedReviveAction, new Action(Init));
	}

	public void Init()
	{
		m_platforms.Clear();
		m_currentPlatform = null;
		PlotPlatforms();
		RaycastMap();
		PostProcessPlatforms();
		inited = true;
		Debug.Log("Revived");
	}

	private void PostProcessPlatforms()
	{
		foreach (KeyValuePair<Guid, Platform> platform in m_platforms)
		{
			platform.Value.PostProcessPlatformPoints();
		}
	}

	private void PlotPlatforms()
	{
		BoxCollider2D[] array = UnityEngine.Object.FindObjectsOfType<BoxCollider2D>();
		foreach (BoxCollider2D boxCollider2D in array)
		{
			if (boxCollider2D.gameObject.layer == LayerMask.GetMask("Player"))
			{
				continue;
			}
			bool flag = false;
			foreach (KeyValuePair<Guid, Platform> platform in m_platforms)
			{
				foreach (BoxCollider2D boxCollider in platform.Value.BoxColliders)
				{
					bool flag2 = false;
					if (boxCollider2D.bounds.Intersects(boxCollider.bounds))
					{
						flag2 = true;
					}
					if (flag2)
					{
						m_platforms[platform.Key].AddCollider(boxCollider2D);
						flag = true;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
			if (!flag)
			{
				Guid key = Guid.NewGuid();
				m_platforms.Add(key, new Platform());
				m_platforms[key].AddCollider(boxCollider2D);
			}
		}
	}

	private void MergePlatforms()
	{
	}

	private void RaycastMap()
	{
		m_layerMask = LayerMask.GetMask("Default");
		Camera component = MainCam.instance.transform.GetComponent<Camera>();
		Vector3 vector = component.ViewportToWorldPoint(new Vector2(0f, 1f));
		Vector3 vector2 = component.ViewportToWorldPoint(new Vector2(1f, 1f));
		m_sampleSize = (vector2.x - vector.x) / (float)m_sampleCount;
		for (float num = vector.x; num < vector2.x; num += m_sampleSize)
		{
			RaycastHit2D[] array = Physics2D.RaycastAll(new Vector3(num, vector.y, 0f), Vector2.down, 9999f, m_layerMask);
			for (int i = 0; i < array.Length; i++)
			{
				RaycastHit2D raycastHit2D = array[i];
				foreach (KeyValuePair<Guid, Platform> platform in m_platforms)
				{
					if (platform.Value.ContainsCollider((BoxCollider2D)raycastHit2D.collider))
					{
						m_platforms[platform.Key].AddPlatformPoint(raycastHit2D.point);
						break;
					}
				}
			}
		}
	}

	private void Update()
	{
		if (inited)
		{
			FindClosestPlatform(base.transform.position);
			Vector2 vector = m_playerAPI.OtherPlayerPosition() - base.transform.position;
			float magnitude = vector.magnitude;
			vector.Normalize();
			Vector2 normalized = (vector + new Vector2(0f, 0.15f)).normalized;
			m_playerAPI.SetAimDirection(normalized);
			Vector2 position = new Vector2(base.transform.position.x, base.transform.position.y) + vector * 1.5f;
			bool num = m_currentPlatform.IsPositionOutsidePlatform(position);
			m_playerAPI.Move(m_playerAPI.TowardsOtherPlayer());
			if (num)
			{
				m_playerAPI.Jump();
			}
			vector.y = 0f;
			vector.Normalize();
			RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position, vector, 0.85f, m_layerMask);
			RaycastHit2D raycastHit2D2 = Physics2D.Raycast(base.transform.position, vector, magnitude, m_layerMask);
			if ((bool)raycastHit2D.collider)
			{
				m_playerAPI.Jump();
			}
			if (raycastHit2D2.collider == null)
			{
				m_playerAPI.Attack();
			}
			m_playerAPI.Block();
		}
	}

	private void FindClosestPlatform(Vector2 position)
	{
		float num = float.MaxValue;
		foreach (KeyValuePair<Guid, Platform> platform in m_platforms)
		{
			float closestDistance = platform.Value.GetClosestDistance(position);
			if (closestDistance < num)
			{
				m_currentPlatform = platform.Value;
				num = closestDistance;
			}
		}
	}
}
