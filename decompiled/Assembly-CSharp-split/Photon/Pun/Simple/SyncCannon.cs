using System.Collections.Generic;
using Photon.Pun.Simple.Pooling;
using Photon.Utilities;
using UnityEngine;

namespace Photon.Pun.Simple;

public class SyncCannon : SyncShootBase, IProjectileCannon
{
	[SerializeField]
	public GameObject projPrefab;

	[SerializeField]
	public Vector3 velocity = new Vector3(0f, 0f, 10f);

	[SerializeField]
	[EnumMask(false, null)]
	public RespondTo terminateOn = (RespondTo)12;

	[SerializeField]
	[EnumMask(false, null)]
	public RespondTo damageOn = (RespondTo)12;

	[Tooltip("Projectiles are advanced (lagCompensate * RTT) ms into the future on non-owner clients. This will better time align projectiles to the local players time frame (For example dodging a projectile locally is more likely to be how the shooter saw events as well). 0 = Fully in shooters time frame and 1 = Fully in the local players time frame.")]
	[Range(0f, 1f)]
	[SerializeField]
	public float lagCompensate = 1f;

	protected static List<NetObject> reusableNetObjects = new List<NetObject>();

	public override int ApplyOrder => 19;

	protected override void Reset()
	{
		base.Reset();
	}

	public override void OnAwake()
	{
		base.OnAwake();
		if (projPrefab == null)
		{
			projPrefab = ProjectileHelpers.GetPlaceholderProj();
			Pool.AddPrefabToPool(projPrefab, 8, 8, null, tidyUp: true);
		}
		else
		{
			Pool.AddPrefabToPool(projPrefab);
		}
	}

	protected override bool Trigger(Frame frame, int subFrameId, float timeshift = 0f)
	{
		Pool pool = Pool.Spawn(projPrefab, origin);
		pool.GetComponent<IContactTrigger>().Proxy = contactTrigger;
		IProjectile component = pool.GetComponent<IProjectile>();
		component.Initialize(this, frame.frameId, subFrameId, velocity, terminateOn, damageOn, lagCompensate * timeshift);
		component.Owner = this;
		return true;
	}
}
