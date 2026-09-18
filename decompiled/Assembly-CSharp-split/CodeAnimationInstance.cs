using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class CodeAnimationInstance
{
	public enum AnimationType
	{
		position,
		scale,
		rectPosition,
		floatNumber,
		rotation
	}

	public enum AnimationUse
	{
		In,
		Out,
		None,
		Boop
	}

	public float animationSpeed = 1f;

	[FoldoutGroup("Random", 0)]
	public float randomSpeedAmount;

	[FoldoutGroup("USE", 0)]
	public bool X = true;

	[FoldoutGroup("USE", 0)]
	public bool Y = true;

	[FoldoutGroup("USE", 0)]
	public bool Z = true;

	[Space(15f)]
	public AnimationType animationType;

	public AnimationUse animationUse;

	public AnimationCurve curve;

	public float multiplier = 1f;

	public Vector3 direction;

	[FoldoutGroup("Events", 0)]
	public UnityEvent startEvent;

	[FoldoutGroup("Events", 0)]
	public UnityEvent timedEvent;

	[FoldoutGroup("Events", 0)]
	public float eventTiming;

	[FoldoutGroup("Events", 0)]
	public UnityEvent endEvent;
}
