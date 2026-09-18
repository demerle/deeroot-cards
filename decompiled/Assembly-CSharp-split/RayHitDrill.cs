using UnityEngine;

public class RayHitDrill : RayHitEffect
{
	public float metersOfDrilling = 1f;

	private MoveTransform move;

	private ProjectileHit proj;

	public bool mainDrill;

	private ChildRPC rpc;

	private int sinceDrill = 10;

	public float speedModFlat = 0.5f;

	public float speedMod = 0.1f;

	private bool done;

	private void Start()
	{
		move = GetComponentInParent<MoveTransform>();
		proj = GetComponentInParent<ProjectileHit>();
		proj.canPushBox = false;
		RayHitDrill[] componentsInChildren = base.transform.root.GetComponentsInChildren<RayHitDrill>();
		if (componentsInChildren.Length == 1)
		{
			mainDrill = true;
		}
		bool flag = false;
		int num = -1;
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].mainDrill)
			{
				flag = true;
				num = i;
			}
		}
		if (flag)
		{
			if (componentsInChildren[num] != this)
			{
				componentsInChildren[num].metersOfDrilling += metersOfDrilling;
			}
		}
		else
		{
			mainDrill = true;
		}
		if (mainDrill)
		{
			rpc = GetComponentInParent<ChildRPC>();
			rpc.childRPCsVector2.Add("DrillStop", RPCA_Deactivate);
		}
	}

	private void Update()
	{
		if (mainDrill && proj.view.IsMine)
		{
			if (sinceDrill > 2 && !proj.isAllowedToSpawnObjects)
			{
				rpc.CallFunction("DrillStop", base.transform.position);
			}
			sinceDrill++;
		}
	}

	public void RPCA_Deactivate(Vector2 tpPos)
	{
		base.transform.position = tpPos;
		proj.isAllowedToSpawnObjects = true;
		move.simulationSpeed /= speedModFlat;
		move.simulationSpeed *= move.localForce.magnitude * speedMod;
		proj.sendCollisions = true;
		base.transform.GetChild(0).gameObject.SetActive(value: false);
		if (metersOfDrilling < 2f)
		{
			metersOfDrilling = -1f;
		}
	}

	private void OnDestroy()
	{
		if (mainDrill && !proj.isAllowedToSpawnObjects)
		{
			Object.Instantiate(GetComponentInChildren<ParticleSystem>(includeInactive: true).gameObject, base.transform.position, base.transform.rotation).SetActive(value: true);
		}
	}

	public override HasToReturn DoHitEffect(HitInfo hit)
	{
		if (!mainDrill)
		{
			return HasToReturn.canContinue;
		}
		if (proj.isAllowedToSpawnObjects)
		{
			base.transform.root.position = hit.point;
			move.simulationSpeed *= speedModFlat;
			move.simulationSpeed /= move.localForce.magnitude * speedMod;
			base.transform.GetChild(0).gameObject.SetActive(value: true);
			base.transform.GetComponentInChildren<TrailRenderer>().Clear();
		}
		sinceDrill = 0;
		proj.isAllowedToSpawnObjects = false;
		proj.sendCollisions = false;
		metersOfDrilling -= move.velocity.magnitude * TimeHandler.deltaTime * move.simulationSpeed;
		if (metersOfDrilling <= 0f)
		{
			done = true;
			return HasToReturn.canContinue;
		}
		if (!hit.transform || (bool)hit.transform.root.GetComponent<Player>())
		{
			return HasToReturn.canContinue;
		}
		return HasToReturn.hasToReturnNow;
	}
}
