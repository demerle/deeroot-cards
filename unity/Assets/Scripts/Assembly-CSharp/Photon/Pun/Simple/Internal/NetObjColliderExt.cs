using System.Collections.Generic;
using Photon.Utilities;
using UnityEngine;

namespace Photon.Pun.Simple.Internal
{
	public static class NetObjColliderExt
	{
		public static readonly List<Component> reusableComponents = new List<Component>();

		public static readonly List<Collider> reusableColliders = new List<Collider>();

		public static readonly List<Collider2D> reusableColliders2D = new List<Collider2D>();

		public static void IndexColliders(this NetObject netObj)
		{
			List<Component> indexedColliders = netObj.indexedColliders;
			Dictionary<Component, int> colliderLookup = netObj.colliderLookup;
			colliderLookup.Clear();
			indexedColliders.Clear();
			netObj.transform.GetNestedComponentsInChildren<Component, NetObject>(reusableComponents);
			int count = reusableComponents.Count;
			for (int i = 0; i < count; i++)
			{
				Component component = reusableComponents[i];
				if ((bool)(component as Collider))
				{
					indexedColliders.Add(component);
				}
				else if ((bool)(component as Collider2D))
				{
					indexedColliders.Add(component);
				}
			}
			int j = 0;
			for (int count2 = indexedColliders.Count; j < count2; j++)
			{
				colliderLookup.Add(indexedColliders[j], j);
			}
			netObj.bitsForColliderIndex = (indexedColliders.Count - 1).GetBitsForMaxValue();
		}

		public static int GetFirstChildCollider(this Transform transform, ref Component firstFoundCollider, bool countTriggers, bool countNonTriggers)
		{
			if (!countTriggers && !countNonTriggers)
			{
				Debug.LogError("Counting Colliders, but args indicate to ignore everything. Set one to true.");
				firstFoundCollider = null;
				return 0;
			}
			transform.GetComponentsInChildren(true, reusableColliders);
			int count = reusableColliders.Count;
			if (count > 0)
			{
				if (countTriggers && countNonTriggers)
				{
					firstFoundCollider = reusableColliders[0];
					return count;
				}
				int num = 0;
				firstFoundCollider = null;
				for (int i = 0; i < count; i++)
				{
					Collider collider = reusableColliders[i];
					if (countTriggers ? collider.isTrigger : (!collider.isTrigger))
					{
						if (firstFoundCollider == null)
						{
							firstFoundCollider = collider;
						}
						num++;
					}
				}
				return num;
			}
			transform.GetComponentsInChildren(true, reusableColliders2D);
			int count2 = reusableColliders2D.Count;
			if (count2 > 0)
			{
				if (countTriggers && countNonTriggers)
				{
					firstFoundCollider = reusableColliders[0];
					return count;
				}
				int num2 = 0;
				firstFoundCollider = null;
				for (int j = 0; j < count2; j++)
				{
					Collider2D collider2D = reusableColliders2D[j];
					if (countTriggers ? collider2D.isTrigger : (!collider2D.isTrigger))
					{
						if (firstFoundCollider == null)
						{
							firstFoundCollider = collider2D;
						}
						num2++;
					}
				}
				return num2;
			}
			firstFoundCollider = null;
			return 0;
		}

		public static int CountChildCollider(this Transform transform, bool countTriggers, bool countNonTriggers)
		{
			if (!countTriggers && !countNonTriggers)
			{
				Debug.LogError("Counting Colliders, but args indicate to ignore everything. Set one to true.");
				return 0;
			}
			transform.GetComponentsInChildren(true, reusableColliders);
			int count = reusableColliders.Count;
			if (count > 0)
			{
				if (countTriggers && countNonTriggers)
				{
					return count;
				}
				int num = 0;
				for (int i = 0; i < count; i++)
				{
					Collider collider = reusableColliders[i];
					if (countTriggers ? collider.isTrigger : (!collider.isTrigger))
					{
						num++;
					}
				}
				return num;
			}
			transform.GetComponentsInChildren(true, reusableColliders2D);
			int count2 = reusableColliders2D.Count;
			if (count2 > 0)
			{
				if (countTriggers && countNonTriggers)
				{
					return count;
				}
				int num2 = 0;
				for (int j = 0; j < count2; j++)
				{
					Collider2D collider2D = reusableColliders2D[j];
					if (countTriggers ? collider2D.isTrigger : (!collider2D.isTrigger))
					{
						num2++;
					}
				}
				return num2;
			}
			return 0;
		}
	}
}
