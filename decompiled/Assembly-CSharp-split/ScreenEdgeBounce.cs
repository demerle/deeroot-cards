using Photon.Pun;
using UnityEngine;

public class ScreenEdgeBounce : MonoBehaviour
{
	private float sinceBounce = 1f;

	private Camera mainCam;

	private RayHitReflect reflect;

	private Vector2 lastNormal;

	private ProjectileHit projHit;

	private bool done;

	private RayHitBulletSound bulletSound;

	private PhotonView view;

	private void Start()
	{
		GetComponentInParent<ChildRPC>().childRPCsVector2Vector2IntInt.Add("ScreenBounce", DoHit);
		view = GetComponentInParent<PhotonView>();
		bulletSound = GetComponentInParent<RayHitBulletSound>();
		projHit = GetComponentInParent<ProjectileHit>();
		ScreenEdgeBounce[] componentsInChildren = base.transform.root.GetComponentsInChildren<ScreenEdgeBounce>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (i > 0)
			{
				Object.Destroy(componentsInChildren[i]);
			}
		}
		mainCam = MainCam.instance.transform.GetComponent<Camera>();
		reflect = GetComponentInParent<RayHitReflect>();
	}

	private void Update()
	{
		if (!view.IsMine || done)
		{
			return;
		}
		Vector3 vector = mainCam.WorldToScreenPoint(base.transform.position);
		vector.x /= Screen.width;
		vector.y /= Screen.height;
		vector = new Vector3(Mathf.Clamp(vector.x, 0f, 1f), Mathf.Clamp(vector.y, 0f, 1f), vector.z);
		if (vector.x != 0f && vector.x != 1f && vector.y != 1f && vector.y != 0f)
		{
			return;
		}
		Vector2 vector2 = Vector2.zero;
		if (vector.x == 0f)
		{
			vector2 = Vector2.right;
		}
		else if (vector.x == 1f)
		{
			vector2 = -Vector2.right;
		}
		if (vector.y == 0f)
		{
			vector2 = Vector2.up;
		}
		else if (vector.y == 1f)
		{
			vector2 = -Vector2.up;
		}
		if (lastNormal == vector2 && Vector2.Angle(vector2, base.transform.forward) < 90f)
		{
			lastNormal = vector2;
			return;
		}
		lastNormal = vector2;
		vector.x *= Screen.width;
		vector.y *= Screen.height;
		RaycastHit2D raycastHit2D = new RaycastHit2D
		{
			normal = vector2,
			point = mainCam.ScreenToWorldPoint(vector)
		};
		int num = -1;
		if ((bool)raycastHit2D.transform)
		{
			PhotonView component = raycastHit2D.transform.root.GetComponent<PhotonView>();
			if ((bool)component)
			{
				num = component.ViewID;
			}
		}
		int intData = -1;
		if (num == -1)
		{
			Collider2D[] componentsInChildren = MapManager.instance.currentMap.Map.GetComponentsInChildren<Collider2D>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if (componentsInChildren[i] == raycastHit2D.collider)
				{
					intData = i;
				}
			}
		}
		GetComponentInParent<ChildRPC>().CallFunction("ScreenBounce", raycastHit2D.point, raycastHit2D.normal, num, intData);
		if (reflect.reflects <= 0)
		{
			done = true;
		}
		sinceBounce = 0f;
	}

	private void DoHit(Vector2 hitPos, Vector2 hitNormal, int viewID = -1, int colliderID = -1)
	{
		HitInfo hitInfo = new HitInfo();
		hitInfo.point = hitPos;
		hitInfo.normal = hitNormal;
		hitInfo.collider = null;
		if (viewID != -1)
		{
			PhotonView photonView = PhotonNetwork.GetPhotonView(viewID);
			hitInfo.collider = photonView.GetComponentInChildren<Collider2D>();
			hitInfo.transform = photonView.transform;
		}
		else if (colliderID != -1)
		{
			hitInfo.collider = MapManager.instance.currentMap.Map.GetComponentsInChildren<Collider2D>()[colliderID];
			hitInfo.transform = hitInfo.collider.transform;
		}
		DynamicParticles.instance.PlayBulletHit(projHit.damage, base.transform, hitInfo, projHit.projectileColor);
		bulletSound.DoHitEffect(hitInfo);
		reflect.DoHitEffect(hitInfo);
	}
}
