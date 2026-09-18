using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAIPhilip : MonoBehaviour
{
	private enum BattleMorale : byte
	{
		Blood,
		Coward,
		Defend
	}

	private enum BattleBehaviour : byte
	{
		Attack,
		HighGround,
		MoveSegments
	}

	private PlayerAPI m_PlayerAPI;

	private Player m_Enemy;

	private BattleMorale m_CurrentMorale;

	private BattleBehaviour m_CurrentBehaviour;

	private Vector2 m_Direction;

	private Vector2 m_TargetPos;

	private Vector2[] m_Boundaries;

	private Vector2[] m_Segments;

	private int m_CurrentSegment;

	private float m_Tick;

	private int m_BehaviourChangeTick;

	private int m_BehaviourTimeMin = 3;

	private int m_BehaviourTimeMax = 8;

	private float m_AttackDistance = 15f;

	private void Awake()
	{
		InitReferences();
		MakeBoundaries();
		NextBehaviour();
	}

	private void InitReferences()
	{
		m_PlayerAPI = GetComponentInParent<PlayerAPI>();
	}

	private void MakeBoundaries()
	{
		int num = 20;
		int num2 = 35;
		Vector2 vector = new Vector2(num2, num);
		Vector2 vector2 = new Vector2(-num2, num);
		Vector2 vector3 = new Vector2(num2, -num);
		Vector2 vector4 = new Vector2(-num2, -num);
		m_Boundaries = new Vector2[4] { vector, vector2, vector3, vector4 };
		m_Segments = new Vector2[4]
		{
			vector / 2f,
			vector2 / 2f,
			vector3 / 2f,
			vector4 / 2f
		};
	}

	private bool CheckForValidEnemy()
	{
		m_Enemy = m_PlayerAPI.GetOtherPlayer();
		return m_Enemy != null;
	}

	private void Start()
	{
		CheckForValidEnemy();
	}

	private void Update()
	{
		if (CheckForValidEnemy())
		{
			CheckMorale();
			CheckDirection();
			CheckGround();
			Move();
			Jump();
			DoAim();
			ShouldAttack();
			ShouldBlock();
			TickBehaviour();
		}
	}

	private void SeedBehaviourChange()
	{
		m_BehaviourChangeTick = UnityEngine.Random.Range(m_BehaviourTimeMin, m_BehaviourTimeMax);
	}

	private void TickBehaviour()
	{
		m_Tick += TimeHandler.deltaTime;
		if (m_Tick >= (float)m_BehaviourChangeTick)
		{
			NextBehaviour();
		}
	}

	private void NextBehaviour()
	{
		ResetTick();
		int length = Enum.GetValues(typeof(BattleBehaviour)).Length;
		m_CurrentBehaviour = (BattleBehaviour)UnityEngine.Random.Range(0, length);
		SeedBehaviourChange();
		CheckBehaviour();
	}

	private void ResetTick()
	{
		m_Tick = 0f;
	}

	private void ShouldBlock()
	{
		if (CheckIncommingBullets())
		{
			m_PlayerAPI.Block();
		}
	}

	private bool CheckIncommingBullets()
	{
		Vector3 vector = m_PlayerAPI.PlayerPosition();
		Vector2 a = new Vector2(vector.x, vector.y);
		float num = 1.5f;
		List<BulletWrapper> allBullets = m_PlayerAPI.GetAllBullets();
		int count = allBullets.Count;
		for (int i = 0; i < count; i++)
		{
			Vector2 vector2 = allBullets[i].projectileHit.transform.position;
			float num2 = Vector2.Distance(a, vector2);
			if (num2 <= num && Vector2.Distance(a, vector2 + allBullets[i].velocity.normalized) < num2)
			{
				return true;
			}
		}
		return false;
	}

	private void ShouldAttack()
	{
		if (CanSee() && Vector3.Distance(m_PlayerAPI.PlayerPosition(), m_PlayerAPI.OtherPlayerPosition()) <= m_AttackDistance)
		{
			m_PlayerAPI.Attack();
		}
	}

	private bool CanSee()
	{
		Vector2 vector = m_PlayerAPI.TowardsOtherPlayer();
		Vector3 vector2 = new Vector3(vector.x, vector.y, 0f);
		vector2.Normalize();
		Collider2D collider = Physics2D.Raycast(base.transform.position + vector2, vector2, 20f).collider;
		if ((bool)collider && (bool)collider.GetComponent<Player>())
		{
			return true;
		}
		return false;
	}

	private void DoAim()
	{
		m_PlayerAPI.SetAimDirection(m_PlayerAPI.TowardsOtherPlayer() + m_PlayerAPI.GetOtherPlayer().data.playerVel.velocity * 0.1f);
	}

	private void Move()
	{
		m_PlayerAPI.Move(m_Direction);
	}

	private void Jump()
	{
		m_PlayerAPI.Jump();
	}

	private void CheckGround()
	{
		Vector2 vector = m_PlayerAPI.PlayerPosition();
		if (!(vector.y < 0f) || m_PlayerAPI.CheckGroundBelow(vector + Vector2.down * 0.5f, 10f))
		{
			return;
		}
		m_Direction = Vector2.right;
		float num = float.PositiveInfinity;
		RaycastHit2D raycastHit2D = Physics2D.Raycast(base.transform.position + Vector3.right, Vector2.right, 5f);
		if ((bool)raycastHit2D.collider)
		{
			num = raycastHit2D.distance;
		}
		raycastHit2D = Physics2D.Raycast(base.transform.position + Vector3.left, Vector2.left, 5f);
		if ((bool)raycastHit2D.collider)
		{
			if (raycastHit2D.distance < num)
			{
				m_Direction = Vector2.left;
			}
		}
		else
		{
			m_Direction = Vector2.right;
		}
	}

	private void CheckDirection()
	{
		if (m_CurrentBehaviour == BattleBehaviour.Attack)
		{
			switch (m_CurrentMorale)
			{
			case BattleMorale.Blood:
				m_Direction = m_PlayerAPI.TowardsOtherPlayer();
				break;
			case BattleMorale.Coward:
				m_Direction = -m_PlayerAPI.TowardsOtherPlayer();
				break;
			case BattleMorale.Defend:
				m_Direction = -m_PlayerAPI.TowardsOtherPlayer();
				break;
			}
		}
		else
		{
			Vector3 vector = m_PlayerAPI.PlayerPosition();
			Vector2 vector2 = new Vector2(vector.x, vector.y);
			m_Direction = m_TargetPos - vector2;
		}
	}

	private void CheckBehaviour()
	{
		switch (m_CurrentBehaviour)
		{
		case BattleBehaviour.HighGround:
			m_TargetPos = m_Segments[0];
			break;
		case BattleBehaviour.MoveSegments:
		{
			int max = m_Segments.Length;
			int num;
			for (num = UnityEngine.Random.Range(0, max); num == m_CurrentSegment; num = UnityEngine.Random.Range(0, max))
			{
			}
			m_CurrentSegment = num;
			m_TargetPos = m_Segments[m_CurrentSegment];
			break;
		}
		case BattleBehaviour.Attack:
			break;
		}
	}

	private void CheckMorale()
	{
		if (m_PlayerAPI.CanShoot())
		{
			m_CurrentMorale = BattleMorale.Blood;
		}
	}
}
