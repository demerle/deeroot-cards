using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAIZorro : MonoBehaviour
{
	public AnimationCurve m_AimCompensastionCurve;

	public LayerMask m_MapMask;

	public LayerMask m_PlayerMask;

	private PlayerAPI api;

	private Camera m_camera;

	private int framesSinceShot;

	private Vector3[] surfacesToHideOn;

	private Vector3 m_CurrentHidePos;

	private float tiemSpentOverDeath;

	private float timeSinceCouldShoot;

	private void Start()
	{
		api = GetComponentInParent<PlayerAPI>();
		m_camera = Camera.main;
		HealthHandler healthHandler = api.player.data.healthHandler;
		healthHandler.delayedReviveAction = (Action)Delegate.Combine(healthHandler.delayedReviveAction, new Action(Init));
	}

	public void Init()
	{
		StopAllCoroutines();
		BakeMapSurfaces();
		StartCoroutine(GetNewPos());
	}

	private void Update()
	{
		framesSinceShot++;
		if (api.CanShoot())
		{
			timeSinceCouldShoot += TimeHandler.deltaTime;
		}
		else
		{
			timeSinceCouldShoot = 0f;
		}
		Vector3 vector = api.OtherPlayerPosition() + (Vector3)api.GetOtherPlayer().data.playerVel.velocity * Vector3.Distance(base.transform.position, api.OtherPlayerPosition()) * 0.01f + Vector3.down * api.GetOtherPlayer().data.playerVel.velocity.y * Vector3.Distance(base.transform.position, api.OtherPlayerPosition()) * 0.005f;
		bool flag = false;
		if (Physics2D.Raycast(base.transform.position, Vector3.down, 18f, m_MapMask).transform == null)
		{
			flag = false;
			tiemSpentOverDeath += TimeHandler.deltaTime;
		}
		else
		{
			flag = true;
			tiemSpentOverDeath = 0f;
		}
		if (api.CanBlock() && Vector3.Distance(base.transform.position, api.OtherPlayerPosition()) > 5f && (flag || tiemSpentOverDeath < 0.25f))
		{
			api.Move(vector - base.transform.position + new Vector3(Mathf.PerlinNoise(Time.time, 0f) * 5f * Mathf.Sin(Time.time * 2f), 0f, 0f));
		}
		else
		{
			api.Move(m_CurrentHidePos - base.transform.position + new Vector3(Mathf.PerlinNoise(Time.time, 0f) * 8f * Mathf.Sin(Time.time * 3f), 0f, 0f));
		}
		api.Jump();
		ShootAt(vector);
		MakeSureToBlock();
	}

	public void MakeSureToBlock()
	{
		BulletWrapper[] array = api.GetAllBullets().ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			float num = Vector3.Distance(array[i].projectileMovement.transform.position, base.transform.position);
			float num2 = Vector3.Angle(array[i].velocity.normalized, base.transform.position - array[i].projectileMovement.transform.position);
			if (num < 1.3f && num2 < 65f && framesSinceShot >= 4)
			{
				api.Block();
			}
		}
	}

	public BulletWrapper GetMostDangerousBullet(BulletWrapper[] bullets, out bool exsists)
	{
		float num = 999999f;
		int num2 = -1;
		for (int i = 0; i < bullets.Length; i++)
		{
			float num3 = Vector3.Distance(bullets[i].projectileMovement.transform.position, base.transform.position);
			if (num3 < num && num3 < 5f)
			{
				num = num3;
				num2 = i;
			}
		}
		if (num2 != -1)
		{
			exsists = true;
			return bullets[num2];
		}
		exsists = false;
		return new BulletWrapper();
	}

	public BulletWrapper[] GetAllBulletsComingAtMe()
	{
		List<BulletWrapper> list = new List<BulletWrapper>();
		BulletWrapper[] array = api.GetAllBullets().ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			if (Vector3.Angle(array[i].velocity.normalized, base.transform.position - array[i].projectileMovement.transform.position) < 35f)
			{
				list.Add(array[i]);
			}
		}
		return list.ToArray();
	}

	public void ShootAt(Vector3 point)
	{
		api.SetAimDirection(GetAimDirForHitting(point));
		api.Attack();
	}

	private Vector2 GetAimDirForHitting(Vector3 point)
	{
		Vector3 vector = point - base.transform.position;
		api.SetAimDirection(vector);
		api.GetMyBullet();
		float time = Mathf.Abs(point.x - base.transform.position.x);
		Vector3 vector2 = point + Vector3.up * m_AimCompensastionCurve.Evaluate(time);
		Debug.DrawLine(point, vector2, Color.red, 0.2f);
		return vector2 - base.transform.position;
	}

	public void BakeMapSurfaces()
	{
		Vector2 vector = m_camera.ViewportToWorldPoint(new Vector3(1f, 1f));
		Vector2 vector2 = m_camera.ViewportToWorldPoint(new Vector3(0f, 1f));
		Vector2 vector3 = m_camera.ViewportToWorldPoint(new Vector3(1f, 0f));
		Vector2 vector4 = m_camera.ViewportToWorldPoint(new Vector3(0f, 0f));
		Vector3 v = m_camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f));
		Debug.DrawLine(vector, v, Color.cyan);
		Debug.DrawLine(vector2, v, Color.cyan);
		Debug.DrawLine(vector3, v, Color.cyan);
		Debug.DrawLine(vector4, v, Color.cyan);
		List<Vector3> list = new List<Vector3>();
		for (int i = 0; i < 360; i++)
		{
			for (int j = 0; j < 40; j++)
			{
				RaycastHit2D raycastHit2D = Physics2D.Raycast(new Vector2(Mathf.Lerp(vector2.x, vector.x, (float)i / 359f), Mathf.Lerp(vector3.y, vector.y, (float)j / 39f)), Vector3.down, 6f, m_MapMask);
				if ((bool)raycastHit2D.transform)
				{
					list.Add(raycastHit2D.point);
				}
			}
		}
		List<Vector3> list2 = new List<Vector3>();
		for (int k = 0; k < list.Count; k++)
		{
			if (Physics2D.OverlapCircleAll(list[k] + Vector3.up * 0.26f, 0.1f, m_MapMask).Length != 0)
			{
				list2.Add(list[k]);
			}
		}
		for (int l = 0; l < list2.Count; l++)
		{
			list.Remove(list2[l]);
		}
		Debug.Log("Points: " + list.Count);
		for (int m = 0; m < list.Count; m++)
		{
			Debug.DrawLine(list[m], list[m] + Vector3.up * 0.2f, Color.magenta, 1000f);
		}
		surfacesToHideOn = list.ToArray();
	}

	private IEnumerator GetNewPos()
	{
		while (true)
		{
			m_CurrentHidePos = GetPosAwayFrom(api.OtherPlayerPosition());
			yield return new WaitForSeconds(4f);
		}
	}

	public Vector3 GetPosAwayFrom(Vector3 point)
	{
		Vector3 result;
		do
		{
			result = surfacesToHideOn[UnityEngine.Random.Range(0, surfacesToHideOn.Length)];
		}
		while (!(Mathf.Abs(result.x - point.x) > 13f));
		return result;
	}
}
