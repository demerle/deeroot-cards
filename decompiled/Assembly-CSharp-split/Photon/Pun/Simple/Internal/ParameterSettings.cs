using System;
using System.Collections.Generic;
using Photon.Compression;
using Photon.Utilities;
using UnityEngine;

namespace Photon.Pun.Simple.Internal;

[Serializable]
public class ParameterSettings
{
	public int hash;

	public AnimatorControllerParameterType paramType;

	public bool include;

	public ParameterInterpolation interpolate;

	public ParameterExtrapolation extrapolate;

	public SmartVar defaultValue;

	public LiteFloatCrusher fcrusher;

	public LiteIntCrusher icrusher;

	private static readonly List<int> rebuiltHashes = new List<int>();

	private static readonly List<ParameterSettings> rebuiltSettings = new List<ParameterSettings>();

	public ParameterSettings(int hash, ParameterDefaults defs, ref int paramCount, AnimatorControllerParameterType paramType)
	{
		this.hash = hash;
		this.paramType = paramType;
		switch (paramType)
		{
		case AnimatorControllerParameterType.Float:
			include = defs.includeFloats;
			interpolate = defs.interpolateFloats;
			extrapolate = defs.extrapolateFloats;
			defaultValue = (float)defs.defaultFloat;
			fcrusher = new LiteFloatCrusher(LiteFloatCompressType.Half16, 0f, 1f, accurateCenter: true);
			break;
		case AnimatorControllerParameterType.Int:
			include = defs.includeInts;
			interpolate = defs.interpolateInts;
			extrapolate = defs.extrapolateInts;
			defaultValue = (int)defs.defaultInt;
			icrusher = new LiteIntCrusher();
			break;
		case AnimatorControllerParameterType.Bool:
			include = defs.includeBools;
			interpolate = ParameterInterpolation.Hold;
			extrapolate = ParameterExtrapolation.Hold;
			defaultValue = (bool)defs.defaultBool;
			break;
		case AnimatorControllerParameterType.Trigger:
			include = defs.includeTriggers;
			interpolate = ParameterInterpolation.Default;
			extrapolate = ParameterExtrapolation.Default;
			defaultValue = (bool)defs.defaultTrigger;
			break;
		}
	}

	public static List<string> RebuildParamSettings(Animator a, ref ParameterSettings[] paraSettings, ref int paramCount, ParameterDefaults defs)
	{
		AnimatorControllerParameter[] parameters = a.parameters;
		rebuiltHashes.Clear();
		rebuiltSettings.Clear();
		bool flag = false;
		paramCount = parameters.Length;
		for (int i = 0; i < paramCount; i++)
		{
			AnimatorControllerParameter animatorControllerParameter = parameters[i];
			int nameHash = animatorControllerParameter.nameHash;
			int hashIndex = GetHashIndex(paraSettings, nameHash);
			if (hashIndex != i)
			{
				flag = true;
			}
			rebuiltHashes.Add(nameHash);
			rebuiltSettings.Add((hashIndex == -1) ? new ParameterSettings(nameHash, defs, ref paramCount, animatorControllerParameter.type) : paraSettings[hashIndex]);
		}
		if (flag)
		{
			paraSettings = rebuiltSettings.ToArray();
		}
		return null;
	}

	private static int GetHashIndex(ParameterSettings[] ps, int lookfor)
	{
		int i = 0;
		for (int num = ps.Length; i < num; i++)
		{
			if (ps[i].hash == lookfor)
			{
				return i;
			}
		}
		return -1;
	}
}
