using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.Simple
{
	public class GenericSpawnPoint : MonoBehaviour
	{
		[Header("Spawn Point Blocked Check")]
		[Tooltip("Select the physics layers for colliders to test against. If 'avoidCollision' is true and any colliders on these layers are blocking the spawn point, will attempt to find the next spawn point that isn't blocked.")]
		public LayerMask layerMask;

		public float blockedCheckRadius = 2f;

		public static readonly List<GenericSpawnPoint> spawns = new List<GenericSpawnPoint>();

		private static int lastPicked;

		private static readonly Collider[] reusable = new Collider[8];

		public bool IsBlocked
		{
			get
			{
				if (Physics.OverlapSphereNonAlloc(base.transform.position, blockedCheckRadius, reusable, layerMask) != 0)
				{
					return true;
				}
				return false;
			}
		}

		private void OnEnable()
		{
			spawns.Add(this);
		}

		private void OnDisable()
		{
			spawns.Remove(this);
		}

		public static Transform GetRandomSpawnPoint(bool avoidCollision = true)
		{
			if (spawns.Count == 0)
			{
				return null;
			}
			int num = Random.Range(0, spawns.Count - 1);
			if (avoidCollision)
			{
				for (int i = 0; i < spawns.Count; i++)
				{
					if (!spawns[(i + num) % spawns.Count].IsBlocked)
					{
						return spawns[(i + num) % spawns.Count].transform;
					}
				}
			}
			return spawns[num].transform;
		}

		public static Transform GetNextSpawnPoint(bool avoidCollision = true)
		{
			if (spawns.Count == 0)
			{
				return null;
			}
			lastPicked = (lastPicked + 1) % spawns.Count;
			if (avoidCollision)
			{
				for (int i = 0; i < spawns.Count; i++)
				{
					int index = (i + lastPicked) % spawns.Count;
					if (!spawns[index].IsBlocked)
					{
						lastPicked = index;
						break;
					}
				}
			}
			return spawns[lastPicked].transform;
		}

		public static Transform GetSpawnPointFromValue(int value)
		{
			if (spawns.Count == 0)
			{
				return null;
			}
			int index = (value + 1) % spawns.Count;
			return spawns[index].transform;
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(base.transform.position, blockedCheckRadius);
		}
	}
}
