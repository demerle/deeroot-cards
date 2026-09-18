using System.Collections;
using System.Linq;
using UnityEngine;

public class RayCastTrail : MonoBehaviour
{
	public LayerMask mask;

	public LayerMask playerMask;

	public LayerMask ignoreWallsMask;

	public int teamID = -1;

	private float timeAtSpawn;

	public float size;

	public float extraSize;

	private MoveTransform move;

	private Vector3 lastPos;

	private RayHit rayHit;

	private Rigidbody2D ignoredRig;

	private void Awake()
	{
		timeAtSpawn = Time.time;
	}

	private void Start()
	{
		rayHit = GetComponent<RayHit>();
		ProjectileHit component = GetComponent<ProjectileHit>();
		if ((bool)component)
		{
			size = Mathf.Clamp(Mathf.Pow(component.damage, 0.85f) / 400f, 0f, 100f) + 0.3f + extraSize;
		}
		move = GetComponent<MoveTransform>();
		lastPos = base.transform.position;
	}

	private void OnEnable()
	{
		lastPos = base.transform.position;
	}

	private void Update()
	{
		RaycastHit2D[] first = Physics2D.RaycastAll(lastPos, base.transform.position - lastPos, Vector3.Distance(base.transform.position, lastPos), mask);
		RaycastHit2D[] second = Physics2D.CircleCastAll(lastPos, size, base.transform.position - lastPos, Vector3.Distance(base.transform.position, lastPos), playerMask);
		RaycastHit2D[] array = first.Concat(second).ToArray();
		RaycastHit2D raycastHit2D = default(RaycastHit2D);
		raycastHit2D.distance = float.PositiveInfinity;
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i] || array[i].transform.root == base.transform.root)
			{
				continue;
			}
			Player component = array[i].transform.root.GetComponent<Player>();
			if ((!component || component.playerID != teamID || !(timeAtSpawn + 0.2f >= Time.time)) && (!ignoredRig || !(ignoredRig == array[i].rigidbody)))
			{
				ProjectileHitSurface component2 = array[i].collider.GetComponent<ProjectileHitSurface>();
				if ((!component2 || component2.HitSurface(HitInfo.GetHitInfo(array[i]), base.gameObject) != 0) && array[i].distance < raycastHit2D.distance)
				{
					raycastHit2D = array[i];
				}
			}
		}
		if ((bool)raycastHit2D.transform)
		{
			rayHit.Hit(HitInfo.GetHitInfo(raycastHit2D));
		}
		if ((bool)GridVisualizer.instance)
		{
			GridVisualizer.instance.BulletCall(base.transform.position);
		}
		lastPos = base.transform.position;
	}

	public void WasBlocked()
	{
		timeAtSpawn = 0f;
	}

	public void MoveRay()
	{
		lastPos = base.transform.position;
	}

	public void IgnoreRigFor(Rigidbody2D rig, float time)
	{
		StartCoroutine(DoIgnoreRigFor(rig, time));
	}

	private IEnumerator DoIgnoreRigFor(Rigidbody2D rig, float time)
	{
		ignoredRig = rig;
		yield return new WaitForSeconds(time);
		ignoredRig = null;
	}
}
