using UnityEngine;

namespace Photon.Pun.Simple;

public class MountSwitcher : NetComponent, IOnPreUpdate, IOnPreSimulate
{
	public KeyCode keycode = KeyCode.M;

	public MountSelector mount = new MountSelector(1);

	protected bool triggered;

	protected SyncState syncState;

	public override void OnAwake()
	{
		base.OnAwake();
		if ((bool)netObj)
		{
			syncState = netObj.GetComponent<SyncState>();
		}
		if (!GetComponent<SyncState>())
		{
			Debug.LogWarning(GetType().Name + " on '" + base.transform.parent.name + "/" + base.name + "' needs to be on the root of NetObject with component " + typeof(SyncState).Name + ". Disabling.");
			netObj.RemoveInterfaces(this);
		}
	}

	public void OnPreUpdate()
	{
		if (Input.GetKeyDown(keycode))
		{
			triggered = true;
		}
	}

	public void OnPreSimulate(int frameId, int subFrameId)
	{
		if (triggered)
		{
			triggered = false;
			Mount currentMount = syncState.CurrentMount;
			if ((object)currentMount != null && currentMount.IsMine)
			{
				Debug.Log(string.Concat("Try change to mount : ", currentMount, " : ", currentMount.IsMine.ToString(), " : ", mount.id));
				syncState.ChangeMount(mount.id);
			}
		}
	}
}
