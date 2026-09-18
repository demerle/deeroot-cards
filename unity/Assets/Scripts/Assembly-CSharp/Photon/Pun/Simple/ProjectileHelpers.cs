using UnityEngine;

namespace Photon.Pun.Simple
{
	public static class ProjectileHelpers
	{
		public static GameObject prefab;

		public static GameObject GetPlaceholderProj()
		{
			if (prefab != null)
			{
				return prefab;
			}
			GameObject gameObject = new GameObject("Projectile Placeholder Prefab");
			gameObject.gameObject.SetActive(false);
			Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
			rigidbody.useGravity = false;
			rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			gameObject.AddComponent<ContactProjectile>();
			gameObject.AddComponent<ContactTrigger>();
			GameObject gameObject2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			gameObject2.transform.parent = gameObject.transform;
			gameObject2.GetComponent<Collider>().isTrigger = true;
			gameObject2.GetComponent<Renderer>().material.color = Color.yellow;
			gameObject2.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
			prefab = gameObject;
			return gameObject;
		}
	}
}
