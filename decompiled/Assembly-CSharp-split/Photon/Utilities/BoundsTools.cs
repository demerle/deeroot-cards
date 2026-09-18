using System.Collections.Generic;
using UnityEngine;

namespace Photon.Utilities;

public static class BoundsTools
{
	public enum BoundsType
	{
		Both,
		MeshRenderer,
		Collider,
		Manual
	}

	private static readonly List<MeshFilter> meshFilters = new List<MeshFilter>();

	private static readonly List<Renderer> meshRenderers = new List<Renderer>();

	private static readonly List<Collider> colliders = new List<Collider>();

	private static readonly List<Collider> validColliders = new List<Collider>();

	private static readonly List<SpriteRenderer> spriteRenderers = new List<SpriteRenderer>();

	private static readonly List<Collider2D> colliders2D = new List<Collider2D>();

	private static readonly List<Collider2D> validColliders2D = new List<Collider2D>();

	public static Bounds CollectMyBounds(this GameObject go, BoundsType factorIn, out int numOfBoundsFound, bool includeChildren = true, bool includeInactive = false)
	{
		if (!go.activeInHierarchy && includeInactive)
		{
			numOfBoundsFound = 0;
			return default(Bounds);
		}
		bool num = factorIn == BoundsType.Both;
		bool flag = num || factorIn == BoundsType.MeshRenderer;
		bool num2 = num || factorIn == BoundsType.Collider;
		meshFilters.Clear();
		meshRenderers.Clear();
		colliders.Clear();
		spriteRenderers.Clear();
		validColliders.Clear();
		validColliders2D.Clear();
		int num3 = 0;
		if (flag && go.activeInHierarchy)
		{
			if (includeChildren)
			{
				go.GetComponentsInChildren(includeInactive, meshRenderers);
				go.GetComponentsInChildren(includeInactive, meshFilters);
				go.GetComponentsInChildren(includeInactive, spriteRenderers);
			}
			else
			{
				go.GetComponents(meshRenderers);
				go.GetComponents(meshFilters);
				go.GetComponents(spriteRenderers);
			}
		}
		if (num2 && go.activeInHierarchy)
		{
			if (includeChildren)
			{
				go.GetComponentsInChildren(includeInactive, colliders);
				go.GetComponentsInChildren(includeInactive, colliders2D);
			}
			else
			{
				go.GetComponents(colliders);
				go.GetComponents(colliders2D);
			}
		}
		for (int i = 0; i < meshFilters.Count; i++)
		{
			Renderer component = meshFilters[i].GetComponent<Renderer>();
			if ((bool)component && (component.enabled || includeInactive) && !meshRenderers.Contains(component))
			{
				meshRenderers.Add(component);
			}
		}
		for (int j = 0; j < colliders.Count; j++)
		{
			if ((colliders[j].enabled || includeInactive) && (bool)colliders[j])
			{
				validColliders.Add(colliders[j]);
			}
		}
		for (int k = 0; k < colliders2D.Count; k++)
		{
			if ((((bool)colliders2D[k] && colliders2D[k].enabled) || includeInactive) && (bool)colliders2D[k])
			{
				validColliders2D.Add(colliders2D[k]);
			}
		}
		numOfBoundsFound = meshRenderers.Count + spriteRenderers.Count + validColliders.Count + validColliders2D.Count;
		if (numOfBoundsFound == 0)
		{
			return default(Bounds);
		}
		Bounds bounds;
		if (meshRenderers.Count > 0)
		{
			bounds = meshRenderers[0].bounds;
		}
		else if (validColliders.Count > 0)
		{
			bounds = validColliders[0].bounds;
		}
		else if (validColliders2D.Count > 0 && (bool)validColliders2D[0])
		{
			bounds = validColliders2D[0].bounds;
		}
		else
		{
			if (spriteRenderers.Count <= 0)
			{
				return default(Bounds);
			}
			bounds = spriteRenderers[0].bounds;
		}
		for (int l = 0; l < spriteRenderers.Count; l++)
		{
			num3++;
			bounds.Encapsulate(spriteRenderers[l].bounds);
		}
		for (int m = 0; m < meshRenderers.Count; m++)
		{
			num3++;
			bounds.Encapsulate(meshRenderers[m].bounds);
		}
		for (int n = 0; n < validColliders.Count; n++)
		{
			num3++;
			bounds.Encapsulate(validColliders[n].bounds);
		}
		for (int num4 = 0; num4 < validColliders2D.Count; num4++)
		{
			num3++;
			if ((bool)validColliders2D[num4])
			{
				bounds.Encapsulate(validColliders2D[num4].bounds);
			}
		}
		return bounds;
	}

	public static Bounds CollectMyBounds(GameObject go, BoundsType factorIn, bool includeChildren = true)
	{
		int numOfBoundsFound;
		return go.CollectMyBounds(factorIn, out numOfBoundsFound, includeChildren);
	}
}
