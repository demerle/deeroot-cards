using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.Simple;

public class AutoMountHitscan : HitscanComponent
{
	protected SyncState syncState;

	private Queue<Mount> foundMounts = new Queue<Mount>();

	public SyncState SyncState => syncState;

	public override void OnAwake()
	{
		base.OnAwake();
		if ((bool)netObj)
		{
			syncState = netObj.GetComponent<SyncState>();
		}
	}

	public override void OnAuthorityChanged(bool isMine, bool controllerChanged)
	{
		base.OnAuthorityChanged(isMine, controllerChanged);
		List<IOnPreSimulate> onPreSimulateCallbacks = netObj.onPreSimulateCallbacks;
		bool flag = onPreSimulateCallbacks.Contains(this);
		base.OnAuthorityChanged(isMine, controllerChanged);
		if (isMine)
		{
			if (!flag)
			{
				onPreSimulateCallbacks.Add(this);
			}
		}
		else if (flag)
		{
			onPreSimulateCallbacks.Remove(this);
		}
	}

	public override void OnPreSimulate(int frameId, int subFrameId)
	{
		if (subFrameId != TickEngineSettings.sendEveryXTick - 1)
		{
			return;
		}
		triggerQueued = true;
		base.OnPreSimulate(frameId, subFrameId);
		if (foundMounts.Count != 0)
		{
			do
			{
				Mount attachTo = foundMounts.Dequeue();
				syncState.SoftMount(attachTo);
			}
			while (foundMounts.Count != 0);
		}
		else
		{
			syncState.SoftMount(null);
		}
	}

	public override bool ProcessHit(Collider hit)
	{
		Mount nestedComponentInParents = hit.transform.GetNestedComponentInParents<Mount, NetObject>();
		if ((bool)nestedComponentInParents)
		{
			foundMounts.Enqueue(nestedComponentInParents);
		}
		return false;
	}
}
