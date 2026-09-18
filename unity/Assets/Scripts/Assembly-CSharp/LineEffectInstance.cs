using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class LineEffectInstance
{
	public enum CurveType
	{
		Add = 0,
		Multiply = 1
	}

	[Space(20f)]
	public bool active = true;

	[FoldoutGroup("Main Curve", 0, Order = 0, Expanded = true)]
	public CurveType curveType;

	[FoldoutGroup("Main Curve", 0, Order = 0, Expanded = true)]
	public AnimationCurve mainCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);

	[FoldoutGroup("Main Curve", 0, Order = 0, Expanded = true)]
	public float mainCurveMultiplier = 1f;

	[FoldoutGroup("Main Curve", 0, Order = 0, Expanded = true)]
	public float mainCurveTiling = 1f;

	[FoldoutGroup("Main Curve", 0, Order = 0, Expanded = true)]
	public bool tilingPerMeter = true;

	[FoldoutGroup("Modifiers", 0)]
	public AnimationCurve effectOverLineCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

	[FoldoutGroup("Animation", 0)]
	public float mainCurveScrollSpeed;

	[FoldoutGroup("Animation", 0)]
	public AnimationCurve effectOverTimeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
}
