using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class CurveAnimationInstance
{
	[FoldoutGroup("$animationUse", 0)]
	public CurveAnimationType animationType;

	[FoldoutGroup("$animationUse", 0)]
	public CurveAnimationUse animationUse;

	[ShowIf("animationUse", CurveAnimationUse.In, true)]
	[FoldoutGroup("$animationUse", 0)]
	public AnimationCurve inCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[ShowIf("animationUse", CurveAnimationUse.Out, true)]
	[FoldoutGroup("$animationUse", 0)]
	public AnimationCurve outCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

	[ShowIf("animationUse", CurveAnimationUse.Boop, true)]
	[FoldoutGroup("$animationUse", 0)]
	public AnimationCurve boopCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

	[HideIf("animationType", CurveAnimationType.Scale, true)]
	[FoldoutGroup("$animationUse", 0)]
	public Vector3 animDirection;

	[FoldoutGroup("$animationUse/Settings", 0)]
	public bool loop;

	[FoldoutGroup("$animationUse/Settings", 0)]
	public bool playOnAwake;

	[FoldoutGroup("$animationUse/Settings", 0)]
	public float speed = 1f;

	[FoldoutGroup("$animationUse/Settings", 0)]
	public float multiplier = 1f;

	[FoldoutGroup("$animationUse/Events", 0)]
	public UnityEvent statEvent;

	[FoldoutGroup("$animationUse/Events", 0)]
	public UnityEvent endEvent;

	[FoldoutGroup("$animationUse/Events", 0)]
	public UnityEvent delayedEvent;

	[FoldoutGroup("$animationUse/Events", 0)]
	public float delay;

	[FoldoutGroup("$animationUse/Debug", 0, Order = 0, Expanded = false)]
	public bool isPlaying;

	[HideInInspector]
	public Coroutine animation;

	public AnimationCurve Curve()
	{
		if (animationUse == CurveAnimationUse.Boop)
		{
			return boopCurve;
		}
		if (animationUse != 0)
		{
			return outCurve;
		}
		return inCurve;
	}
}
