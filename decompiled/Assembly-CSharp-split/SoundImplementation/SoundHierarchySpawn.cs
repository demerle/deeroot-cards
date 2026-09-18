using System;
using Sonigon;
using UnityEngine;

namespace SoundImplementation;

[Serializable]
public class SoundHierarchySpawn
{
	public SoundPolyGrouping soundPolyGrouping = SoundPolyGrouping.perPlayer;

	[Header("Sound Parent")]
	public SoundEvent soundParent;

	public bool soundParentVelocityToIntensity;

	public SoundEvent soundParentStopOnDisable;

	[Header("Sound Child")]
	public SoundEvent soundChild;

	public bool soundChildVelocityToIntensity;

	[Header("Sound Child Child")]
	public SoundEvent soundChildChild;

	public SoundEvent soundChildChildStopOnDisable;

	public bool IfAnySoundIsNotNull()
	{
		if (soundParent != null || soundParentStopOnDisable != null || soundChild != null || soundChildChild != null || soundChildChildStopOnDisable != null)
		{
			return true;
		}
		return false;
	}
}
