using System;
using System.Collections.Generic;
using Photon.Pun.Simple.Pooling;
using UnityEngine;

namespace Photon.Pun.Simple
{
	public static class GenericHitscanExt
	{
		public static Collider[] reusableColliderArray = new Collider[64];

		public static RaycastHit[] reusableRayHitArray = new RaycastHit[64];

		public static List<NetworkHit> reusableHitscanHitList = new List<NetworkHit>();

		public static List<NetObject> reusableNetObjectsList = new List<NetObject>();

		private static GameObject DebugSpherePrefab;

		private static GameObject DebugCylinderPrefab;

		private static GameObject DebugCubePrefab;

		private static readonly Dictionary<int, int> reusableGameObjIntDict = new Dictionary<int, int>();

		private static GameObject SetUpDebugPrimitive(this GameObject go, string name, bool createCylinderChild = false)
		{
			go.name = name;
			go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
			UnityEngine.Object.DontDestroyOnLoad(go);
			Collider component;
			if (createCylinderChild)
			{
				GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
				gameObject.GetComponent<Renderer>().material.color = Color.yellow;
				gameObject.transform.parent = go.transform;
				gameObject.transform.eulerAngles = new Vector3(90f, 0f, 0f);
				component = gameObject.GetComponent<Collider>();
			}
			else
			{
				component = go.GetComponent<Collider>();
			}
			UnityEngine.Object.DestroyImmediate(component);
			Renderer component2 = go.GetComponent<Renderer>();
			if ((bool)component2)
			{
				component2.material.color = Color.yellow;
			}
			return go;
		}

		private static void CreateDebugPrimitives()
		{
			DebugSpherePrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere).SetUpDebugPrimitive("DebugSpherePrefab");
			Pool.AddPrefabToPool(DebugSpherePrefab.gameObject, 2, 2, null, true);
			DebugCubePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube).SetUpDebugPrimitive("DebugCubePrefab");
			Pool.AddPrefabToPool(DebugCubePrefab.gameObject, 2, 2, null, true);
			DebugCylinderPrefab = new GameObject().SetUpDebugPrimitive("DebugCylinderPrefab", true);
			Pool.AddPrefabToPool(DebugCylinderPrefab.gameObject, 4, 4, null, true);
		}

		public static void VisualizeHitscan(this HitscanDefinition hd, Transform origin, float duration = 0.5f)
		{
			if (DebugSpherePrefab == null)
			{
				CreateDebugPrimitives();
			}
			switch (hd.hitscanType)
			{
			case HitscanType.Raycast:
			{
				Vector3 pos10 = (hd.useOffset ? (origin.position + origin.TransformDirection(hd.offset1) + origin.forward * hd.distance * 0.5f) : (origin.position + origin.forward * hd.distance * 0.5f));
				Pool.Spawn(DebugCylinderPrefab, pos10, origin.rotation, new Vector3(0.1f, 0.1f, hd.distance * 0.5f), duration);
				break;
			}
			case HitscanType.BoxCast:
			{
				Vector3 vector10 = (hd.useOffset ? (origin.position + origin.TransformDirection(hd.offset1)) : origin.position);
				Vector3 vector11 = (hd.useOffset ? (origin.position + origin.TransformDirection(hd.offset1) + origin.forward * hd.distance) : (origin.position + origin.forward * hd.distance));
				Vector3 pos12 = vector10 + (vector11 - vector10) * 0.5f;
				Quaternion rot2 = Quaternion.Euler(origin.eulerAngles + hd.orientation);
				Vector3 scl2 = hd.halfExtents * 2f;
				Pool.Spawn(DebugCubePrefab, vector11, rot2, scl2, duration);
				Pool.Spawn(DebugCylinderPrefab, pos12, Quaternion.LookRotation(vector11 - vector10, Vector3.up), new Vector3(0.1f, 0.1f, hd.distance * 0.5f), duration);
				break;
			}
			case HitscanType.SphereCast:
			{
				Vector3 pos3 = (hd.useOffset ? (origin.position + origin.TransformDirection(hd.offset1)) : origin.position);
				Vector3 pos4 = (hd.useOffset ? (origin.position + origin.TransformDirection(hd.offset1) + origin.forward * hd.distance * 0.5f) : (origin.position + origin.forward * hd.distance * 0.5f));
				Vector3 pos5 = (hd.useOffset ? (origin.position + origin.TransformDirection(hd.offset1) + origin.forward * hd.distance) : (origin.position + origin.forward * hd.distance));
				float num2 = hd.radius * 2f;
				Pool.Spawn(DebugSpherePrefab, pos3, Quaternion.identity, new Vector3(num2, num2, num2), duration);
				Pool.Spawn(DebugSpherePrefab, pos5, Quaternion.identity, new Vector3(num2, num2, num2), duration);
				Pool.Spawn(DebugCylinderPrefab, pos4, origin.rotation, new Vector3(num2, num2, hd.distance * 0.5f), duration);
				break;
			}
			case HitscanType.CapsuleCast:
			{
				Vector3 vector4 = origin.position + origin.TransformDirection(hd.offset1);
				Vector3 vector5 = origin.position + origin.TransformDirection(hd.offset2);
				Vector3 vector6 = origin.forward * hd.distance;
				Vector3 vector7 = vector6 * 0.5f;
				Vector3 pos6 = vector4 + vector7;
				Vector3 pos7 = vector5 + vector7;
				Vector3 vector8 = vector4 + vector6;
				Vector3 vector9 = vector5 + vector6;
				Vector3 pos8 = vector4 + (vector5 - vector4) * 0.5f;
				Vector3 pos9 = vector8 + (vector9 - vector8) * 0.5f;
				float num3 = hd.radius * 2f;
				Pool.Spawn(DebugSpherePrefab, vector4, Quaternion.identity, new Vector3(num3, num3, num3), duration);
				Pool.Spawn(DebugSpherePrefab, vector8, Quaternion.identity, new Vector3(num3, num3, num3), duration);
				Pool.Spawn(DebugSpherePrefab, vector5, Quaternion.identity, new Vector3(num3, num3, num3), duration);
				Pool.Spawn(DebugSpherePrefab, vector9, Quaternion.identity, new Vector3(num3, num3, num3), duration);
				Pool.Spawn(DebugCylinderPrefab, pos6, origin.rotation, new Vector3(num3, num3, hd.distance * 0.5f), duration);
				Pool.Spawn(DebugCylinderPrefab, pos7, origin.rotation, new Vector3(num3, num3, hd.distance * 0.5f), duration);
				float z = Vector3.Magnitude(vector5 - vector4) * 0.5f;
				Pool.Spawn(DebugCylinderPrefab, pos8, Quaternion.LookRotation(vector5 - vector4, Vector3.up), new Vector3(num3, num3, z), duration);
				Pool.Spawn(DebugCylinderPrefab, pos9, Quaternion.LookRotation(vector9 - vector8, Vector3.up), new Vector3(num3, num3, z), duration);
				break;
			}
			case HitscanType.OverlapSphere:
			{
				Vector3 pos11 = (hd.useOffset ? (origin.position + hd.offset1) : origin.position);
				Quaternion rotation2 = origin.rotation;
				float num4 = hd.radius * 2f;
				Pool.Spawn(DebugSpherePrefab, pos11, rotation2, new Vector3(num4, num4, num4), duration);
				break;
			}
			case HitscanType.OverlapBox:
			{
				Vector3 pos2 = (hd.useOffset ? (origin.position + hd.offset1) : origin.position);
				Quaternion rot = Quaternion.Euler(origin.eulerAngles + hd.orientation);
				Vector3 scl = hd.halfExtents * 2f;
				Pool.Spawn(DebugCubePrefab, pos2, rot, scl, duration);
				break;
			}
			case HitscanType.OverlapCapsule:
			{
				Quaternion rotation = origin.rotation;
				float num = hd.radius * 2f;
				Vector3 vector = origin.TransformPoint(hd.offset1);
				Vector3 vector2 = origin.TransformPoint(hd.offset2);
				Vector3 vector3 = vector2 - vector;
				Vector3 pos = vector + vector3 * 0.5f;
				Pool.Spawn(DebugSpherePrefab, vector, rotation, new Vector3(num, num, num), duration);
				Pool.Spawn(DebugSpherePrefab, vector2, rotation, new Vector3(num, num, num), duration);
				Pool.Spawn(DebugCylinderPrefab, pos, Quaternion.LookRotation(vector3, Vector3.up), new Vector3(num, num, vector3.magnitude * 0.5f), duration);
				break;
			}
			}
		}

		public static int GenericHitscanNonAlloc(this HitscanDefinition hd, Transform origin, out RaycastHit[] rayhits, out Collider[] hits, ref int nearestIndex, bool showDebugWidgets = false, bool useSecondaryRealm = false)
		{
			hits = reusableColliderArray;
			rayhits = reusableRayHitArray;
			return hd.GenericHitscanNonAlloc(origin, ref reusableColliderArray, ref reusableRayHitArray, ref nearestIndex, showDebugWidgets, useSecondaryRealm);
		}

		public static int GenericHitscanNonAlloc(this HitscanDefinition hd, Transform origin, NetObject ownerNetObj, ref List<NetworkHit> hitscanHits, ref int nearestIndex, bool showDebugWidgets = false, bool useSecondaryRealm = false)
		{
			if (hitscanHits == null)
			{
				hitscanHits = reusableHitscanHitList;
			}
			int nearestIndex2 = -1;
			nearestIndex = -1;
			int num = hd.GenericHitscanNonAlloc(origin, ref reusableColliderArray, ref reusableRayHitArray, ref nearestIndex2, showDebugWidgets, useSecondaryRealm);
			reusableGameObjIntDict.Clear();
			hitscanHits.Clear();
			if (num > 0)
			{
				for (int i = 0; i < num; i++)
				{
					Collider collider = reusableColliderArray[i];
					List<NetObject> list = reusableNetObjectsList;
					collider.transform.GetNestedComponentsInParents(list);
					int count = list.Count;
					if (count == 0)
					{
						continue;
					}
					bool flag = false;
					for (int j = 0; j < count; j++)
					{
						if ((object)list[j] == ownerNetObj)
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						continue;
					}
					NetObject netObject = list[0];
					int viewID = netObject.ViewID;
					IContactGroupsAssign component = collider.GetComponent<IContactGroupsAssign>();
					int num2 = ((component != null) ? component.Mask : 0);
					int value;
					bool num3 = reusableGameObjIntDict.TryGetValue(viewID, out value);
					int colliderId = netObject.colliderLookup[collider];
					if (num3)
					{
						NetworkHit networkHit = hitscanHits[value];
						hitscanHits[value] = new NetworkHit(networkHit.netObjId, networkHit.hitMask | num2, colliderId);
						if (i == nearestIndex2)
						{
							nearestIndex = value;
						}
					}
					else
					{
						if (i == nearestIndex2)
						{
							nearestIndex = hitscanHits.Count;
						}
						hitscanHits.Add(new NetworkHit(viewID, num2, colliderId));
						reusableGameObjIntDict.Add(viewID, hitscanHits.Count - 1);
					}
				}
			}
			return reusableGameObjIntDict.Count;
		}

		public static int GenericHitscanNonAlloc(this HitscanDefinition hd, Transform origin, ref Collider[] hits, ref RaycastHit[] rayhits, ref int nearestIndex, bool showDebugWidgets = false, bool useSecondaryRealm = false)
		{
			if (showDebugWidgets)
			{
				hd.VisualizeHitscan(origin);
			}
			if (hits == null)
			{
				hits = reusableColliderArray;
			}
			if (rayhits == null)
			{
				rayhits = reusableRayHitArray;
			}
			HitscanType hitscanType = hd.hitscanType;
			Vector3 vector = (hd.useOffset ? origin.TransformPoint(hd.offset1) : origin.position);
			LayerMask layerMask = hd.layerMask;
			int num;
			switch (hitscanType)
			{
			case HitscanType.Raycast:
				num = Physics.RaycastNonAlloc(origin.position, origin.forward, rayhits, hd.distance, layerMask);
				break;
			case HitscanType.SphereCast:
				num = Physics.SphereCastNonAlloc(new Ray(vector, origin.forward), hd.radius, rayhits, hd.distance, layerMask);
				break;
			case HitscanType.BoxCast:
				num = Physics.BoxCastNonAlloc(vector, hd.halfExtents, origin.forward, rayhits, Quaternion.Euler(origin.eulerAngles + hd.orientation), hd.distance, layerMask);
				break;
			case HitscanType.CapsuleCast:
				num = Physics.CapsuleCastNonAlloc(origin.TransformPoint(hd.offset1), origin.TransformPoint(hd.offset2), hd.radius, origin.forward, rayhits, hd.distance, layerMask);
				break;
			case HitscanType.OverlapSphere:
				num = Physics.OverlapSphereNonAlloc(vector, hd.radius, hits, layerMask);
				break;
			case HitscanType.OverlapBox:
				num = Physics.OverlapBoxNonAlloc(vector, hd.halfExtents, hits, Quaternion.Euler(origin.eulerAngles + hd.orientation), layerMask);
				break;
			case HitscanType.OverlapCapsule:
				num = Physics.OverlapCapsuleNonAlloc(origin.TransformPoint(hd.offset1), origin.TransformPoint(hd.offset2), hd.radius, hits, layerMask);
				break;
			default:
				num = 0;
				break;
			}
			nearestIndex = -1;
			if (num == 0)
			{
				return num;
			}
			bool nearestOnly = hd.nearestOnly;
			float num2 = float.PositiveInfinity;
			if (hitscanType.IsOverlap())
			{
				nearestIndex = -1;
				return num;
			}
			if (nearestOnly)
			{
				for (int i = 0; i < num; i++)
				{
					RaycastHit raycastHit = rayhits[i];
					float distance = raycastHit.distance;
					if (distance < num2)
					{
						num2 = distance;
						nearestIndex = i;
					}
				}
				hits[0] = rayhits[nearestIndex].collider;
				return 1;
			}
			for (int j = 0; j < num; j++)
			{
				RaycastHit raycastHit2 = rayhits[j];
				float distance2 = raycastHit2.distance;
				if (distance2 < num2)
				{
					num2 = distance2;
					nearestIndex = j;
				}
				if (!nearestOnly)
				{
					hits[j] = rayhits[j].collider;
				}
			}
			if (nearestOnly)
			{
				hits[0] = rayhits[nearestIndex].collider;
				return 1;
			}
			return num;
		}

		[Obsolete("Haven't reworked this for new physx yet.")]
		public static int GenericCastNonAlloc(this Transform srcT, Collider[] hits, RaycastHit[] rayhits, float distance, float radius, int mask, Quaternion orientation, bool useOffset, Vector3 offset1, Vector3 offset2, HitscanType hitscanType)
		{
			Vector3 vector = (useOffset ? (srcT.position + srcT.TransformDirection(offset1)) : srcT.position);
			int num;
			switch (hitscanType)
			{
			case HitscanType.Raycast:
				num = Physics.RaycastNonAlloc(new Ray(vector, srcT.forward), rayhits, distance, mask);
				break;
			case HitscanType.SphereCast:
				num = Physics.SphereCastNonAlloc(new Ray(vector, srcT.forward), radius, rayhits, distance, mask);
				break;
			case HitscanType.BoxCast:
				num = Physics.BoxCastNonAlloc(vector, offset2, srcT.forward, rayhits, orientation, distance, mask);
				break;
			case HitscanType.CapsuleCast:
				num = Physics.CapsuleCastNonAlloc(srcT.TransformPoint(offset1), srcT.TransformPoint(offset2), radius, srcT.forward, rayhits, distance, mask);
				break;
			case HitscanType.OverlapSphere:
				num = Physics.OverlapSphereNonAlloc(vector, radius, hits, mask);
				break;
			case HitscanType.OverlapBox:
				num = Physics.OverlapBoxNonAlloc(vector, offset2, hits, orientation, mask);
				break;
			case HitscanType.OverlapCapsule:
				num = Physics.OverlapCapsuleNonAlloc(srcT.TransformPoint(offset1), srcT.TransformPoint(offset2), radius, hits, mask);
				break;
			default:
				num = 0;
				break;
			}
			if (hitscanType.IsCast())
			{
				for (int i = 0; i < num; i++)
				{
					hits[i] = rayhits[i].collider;
				}
			}
			return num;
		}
	}
}
