using System;
using Photon.Utilities;

namespace Photon.Pun.Simple.Internal;

[Serializable]
public class ParameterDefaults
{
	public ParameterInterpolation interpolateFloats = ParameterInterpolation.Hold;

	public ParameterInterpolation interpolateInts = ParameterInterpolation.Hold;

	public ParameterExtrapolation extrapolateFloats = ParameterExtrapolation.Hold;

	public ParameterExtrapolation extrapolateInts = ParameterExtrapolation.Hold;

	public ParameterExtrapolation extrapolateBools = ParameterExtrapolation.Hold;

	public ParameterExtrapolation extrapolateTriggers;

	public bool includeFloats = true;

	public bool includeInts = true;

	public bool includeBools = true;

	public bool includeTriggers;

	public SmartVar defaultFloat = 0f;

	public SmartVar defaultInt = 0;

	public SmartVar defaultBool = false;

	public SmartVar defaultTrigger = false;
}
