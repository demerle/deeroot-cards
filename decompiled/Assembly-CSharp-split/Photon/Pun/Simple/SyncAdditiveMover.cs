using System;
using UnityEngine;

namespace Photon.Pun.Simple;

public class SyncAdditiveMover : NetComponent, ITransformController, IOnPreUpdate, IOnPreSimulate
{
	[Serializable]
	public class TRSDefinition : TRSDefinitionBase
	{
		public Vector3 addVector = new Vector3(0f, 0f, 0f);
	}

	[HideInInspector]
	public TRSDefinition posDef = new TRSDefinition();

	[HideInInspector]
	public TRSDefinition rotDef = new TRSDefinition();

	[HideInInspector]
	public TRSDefinition sclDef = new TRSDefinition();

	public bool HandlesInterpolation => true;

	public bool HandlesExtrapolation => true;

	public void OnPreSimulate(int frameId, int subFrameId)
	{
		if (base.isActiveAndEnabled && (!photonView || photonView.IsMine))
		{
			AddVector();
		}
	}

	public void OnPreUpdate()
	{
		if (base.isActiveAndEnabled)
		{
			AddVector();
		}
	}

	private void AddVector()
	{
		float mixedDeltaTime = DoubleTime.mixedDeltaTime;
		base.transform.localScale += sclDef.addVector * mixedDeltaTime;
		if (rotDef.local)
		{
			base.transform.localEulerAngles += rotDef.addVector * mixedDeltaTime;
		}
		else
		{
			base.transform.eulerAngles += rotDef.addVector * mixedDeltaTime;
		}
		base.transform.Translate(posDef.addVector * mixedDeltaTime, posDef.local ? Space.Self : Space.World);
	}
}
