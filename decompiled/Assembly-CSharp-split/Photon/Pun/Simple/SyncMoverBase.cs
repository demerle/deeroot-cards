using System;
using UnityEngine;

namespace Photon.Pun.Simple;

[Serializable]
public abstract class SyncMoverBase<TTRSDef, TFrame> : SyncObject<TFrame>, ITransformController, IOnPreSimulate, IOnPreUpdate where TTRSDef : TRSDefinitionBase, new() where TFrame : FrameBase, new()
{
	public enum MovementRelation
	{
		Absolute,
		Relative
	}

	[HideInInspector]
	public TTRSDef posDef = new TTRSDef();

	[HideInInspector]
	public TTRSDef rotDef = new TTRSDef();

	[HideInInspector]
	public TTRSDef sclDef = new TTRSDef();

	protected Rigidbody rb;

	protected Rigidbody2D rb2d;

	[NonSerialized]
	public SyncTransform syncTransform;

	public virtual bool HandlesInterpolation => true;

	public virtual bool HandlesExtrapolation => true;

	public override void OnAwakeInitialize(bool isNetObject)
	{
		if (!isNetObject)
		{
			NetMasterCallbacks.onPreSimulates.Add(this);
			NetMasterCallbacks.onPreUpdates.Add(this);
		}
		rb = GetComponent<Rigidbody>();
		rb2d = GetComponent<Rigidbody2D>();
		if (((bool)rb && !rb.isKinematic) || ((bool)rb2d && !rb2d.isKinematic))
		{
			Debug.LogWarning(GetType().Name + " doesn't work with non-kinematic rigidbodies. Setting to kinematic.");
			if ((bool)rb)
			{
				rb.isKinematic = true;
			}
			else
			{
				rb2d.isKinematic = true;
			}
		}
		syncTransform = GetComponent<SyncTransform>();
		Recalculate();
	}

	public override void OnStartInitialize(bool isNetObject)
	{
		InitializeTRS(posDef, TRS.Position);
		InitializeTRS(rotDef, TRS.Rotation);
		InitializeTRS(sclDef, TRS.Scale);
	}

	private void OnDestroy()
	{
		if (!netObj)
		{
			NetMasterCallbacks.onPreSimulates.Remove(this);
			NetMasterCallbacks.onPreUpdates.Remove(this);
		}
	}

	public virtual void Recalculate()
	{
	}

	protected abstract void InitializeTRS(TTRSDef def, TRS type);

	public abstract void OnPreSimulate(int frameId, int subFrameId);

	public abstract void OnPreUpdate();
}
