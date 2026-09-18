using System;
using System.Collections;
using Photon.Pun;
using Sonigon;
using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
	[Header("Sounds")]
	public SoundEvent soundBounce;

	[Header("Settings")]
	public LayerMask mask;

	private Vector2 lastPos;

	public bool checkForGoThroughWall = true;

	private int ignoreWallFor;

	private Collider2D col;

	private CircleCollider2D cirCol;

	private PlayerVelocity vel;

	private CharacterData data;

	public Action<Vector2, Vector2, Player> collideWithPlayerAction;

	public float bounceTreshold = 1f;

	private bool isBounce;

	public void IgnoreWallForFrames(int frames)
	{
		ignoreWallFor = frames;
	}

	private void Start()
	{
		data = GetComponent<CharacterData>();
		col = GetComponent<Collider2D>();
		cirCol = GetComponent<CircleCollider2D>();
		vel = GetComponent<PlayerVelocity>();
	}

	private void FixedUpdate()
	{
		if (checkForGoThroughWall && ignoreWallFor <= 0)
		{
			RaycastHit2D raycastHit2D = default(RaycastHit2D);
			RaycastHit2D[] array = Physics2D.RaycastAll(lastPos, (Vector2)base.transform.position - lastPos, Vector2.Distance(base.transform.position, lastPos), mask);
			for (int i = 0; i < array.Length; i++)
			{
				if (!(array[i].transform.root == base.transform.root))
				{
					Debug.DrawLine(lastPos, array[i].point, Color.green, 1f);
					if (!(Vector2.Angle(array[i].normal, (Vector2)base.transform.position - lastPos) < 90f) && (!raycastHit2D.transform || array[i].distance < raycastHit2D.distance))
					{
						raycastHit2D = array[i];
					}
				}
			}
			if ((bool)raycastHit2D)
			{
				base.transform.position = raycastHit2D.point + raycastHit2D.normal * 0.5f;
				if (data.healthHandler.flyingFor > 0f)
				{
					DoBounce(raycastHit2D);
				}
			}
		}
		ignoreWallFor--;
		lastPos = base.transform.position;
		float num = cirCol.radius * base.transform.localScale.x;
		float num2 = cirCol.radius * base.transform.localScale.x * 0.75f;
		RaycastHit2D[] array2 = Physics2D.CircleCastAll(lastPos, num, (Vector2)base.transform.position - lastPos, Vector2.Distance(base.transform.position, lastPos), mask);
		for (int j = 0; j < array2.Length; j++)
		{
			if (array2[j].transform.root == base.transform.root)
			{
				continue;
			}
			Vector2 vector = base.transform.position;
			Vector2 point = array2[j].point;
			float num3 = Vector2.Distance(vector, point);
			Vector2 normalized = (vector - point).normalized;
			float value = num + (0f - num3);
			float value2 = num2 + (0f - num3);
			value = Mathf.Clamp(value, 0f, 10f);
			value2 = Mathf.Clamp(value2, 0f, 10f);
			NetworkPhysicsObject component = array2[j].transform.GetComponent<NetworkPhysicsObject>();
			if ((bool)component)
			{
				component.Push(data);
			}
			if (vel.simulated || !vel.isKinematic)
			{
				vel.transform.position += (Vector3)normalized * value2;
				if (Mathf.Abs(normalized.y) < 0.45f && Mathf.Abs(data.input.direction.x) > 0.1f && Vector3.Angle(data.input.direction, normalized) > 90f)
				{
					data.TouchWall(normalized, point);
				}
				vel.velocity += normalized * value * 10f * TimeHandler.timeScale;
				vel.velocity -= vel.velocity * value * 1f * TimeHandler.timeScale;
			}
			Player componentInParent = array2[j].transform.GetComponentInParent<Player>();
			if (componentInParent != null && collideWithPlayerAction != null)
			{
				collideWithPlayerAction(point, value * normalized, componentInParent);
			}
			if (data.healthHandler.flyingFor > 0f)
			{
				DoBounce(array2[j]);
			}
		}
		lastPos = base.transform.position;
	}

	private void DoBounce(RaycastHit2D hit)
	{
		if (!(Vector2.Angle(data.playerVel.velocity, hit.normal) < 90f) && !isBounce && data.view.IsMine && data.playerVel.velocity.magnitude > bounceTreshold)
		{
			data.view.RPC("RPCADoBounce", RpcTarget.All, hit.normal, base.transform.position);
		}
	}

	[PunRPC]
	private void RPCADoBounce(Vector2 hit, Vector3 playerPos)
	{
		base.transform.position = playerPos;
		StartCoroutine(IDoBounce(Vector2.Reflect(data.playerVel.velocity, hit)));
		SoundManager.Instance.Play(soundBounce, base.transform);
	}

	public IEnumerator IDoBounce(Vector2 targetVel)
	{
		isBounce = true;
		data.stunHandler.AddStun(0.2f);
		data.healthHandler.CallTakeDamage(targetVel.normalized * 5f, base.transform.position);
		GamefeelManager.instance.AddGameFeel(targetVel.normalized * 4f);
		yield return new WaitForSeconds(0.25f);
		data.playerVel.velocity = targetVel;
		isBounce = false;
	}

	private void OnDisable()
	{
		isBounce = false;
	}
}
