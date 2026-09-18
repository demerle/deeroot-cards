using Photon.Utilities;
using UnityEngine;

namespace Photon.Pun.Simple;

[HelpURL("https://doc.photonengine.com/en-us/pun/current/gameplay/simple/simpleoverview")]
public class NetMasterLate : MonoBehaviour
{
	public static NetMasterLate single;

	private void Awake()
	{
		if ((bool)single && single != this)
		{
			Object.Destroy(single);
		}
		single = this;
		Object.DontDestroyOnLoad(this);
	}

	private void FixedUpdate()
	{
		if (SettingsScriptableObject<TickEngineSettings>.single.enableTickEngine && (NetObject.activeControlledNetObjs.Count != 0 || NetObject.activeUncontrolledNetObjs.Count != 0))
		{
			NetMasterCallbacks.OnPreSimulateCallbacks(NetMaster.CurrentFrameId, NetMaster.CurrentSubFrameId);
		}
	}

	private void Update()
	{
		if (SettingsScriptableObject<TickEngineSettings>.single.enableTickEngine && (NetObject.activeControlledNetObjs.Count != 0 || NetObject.activeUncontrolledNetObjs.Count != 0))
		{
			NetMasterCallbacks.OnPostUpdateCallbacks();
		}
	}

	private void LateUpdate()
	{
		if (SettingsScriptableObject<TickEngineSettings>.single.enableTickEngine && (NetObject.activeControlledNetObjs.Count != 0 || NetObject.activeUncontrolledNetObjs.Count != 0))
		{
			NetMasterCallbacks.OnPostLateUpdateCallbacks();
		}
	}
}
