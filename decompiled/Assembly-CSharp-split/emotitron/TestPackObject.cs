using Photon.Compression;
using Photon.Pun.Simple;
using UnityEngine;

namespace emotitron;

[PackObject(DefaultKeyRate.Every, defaultInclusion = DefaultPackInclusion.Explicit)]
public class TestPackObject : NetComponent, IOnPreSimulate, IOnInterpolate
{
	[SyncHalfFloat(IndicatorBit.None, KeyRate.UseDefault, snapshotCallback = "SnapshotHook", applyCallback = "RotationHook", setValueTiming = SetValueTiming.BeforeCallback, interpolate = true, keyRate = KeyRate.Every)]
	public float rotation;

	[SyncRangedInt(-1, 2, IndicatorBits.None, KeyRate.UseDefault)]
	public int intoroboto;

	public void RotationHook(float newrot, float oldrot)
	{
		base.transform.localEulerAngles = new Vector3(0f, rotation, 0f);
	}

	public void SnapshotHook(float snap, float targ)
	{
	}

	public void OnPreSimulate(int frameId, int subFrameId)
	{
		if (photonView.IsMine)
		{
			rotation = (Mathf.Sin(Time.time) + 0.5f) * 120f;
			base.transform.localEulerAngles = new Vector3(0f, rotation, 0f);
		}
	}

	public void FixedUpdate()
	{
	}

	private void Update()
	{
	}

	public bool OnInterpolate(int snapFrameId, int targFrameId, float t)
	{
		if (!base.PhotonView.IsMine)
		{
			base.transform.localEulerAngles = new Vector3(0f, rotation, 0f);
		}
		return true;
	}
}
