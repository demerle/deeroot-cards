using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class ObjectParticle
{
	public float size = 1f;

	public AnimationCurve sizeOverTime = AnimationCurve.Linear(0f, 1f, 1f, 1f);

	public float lifetime = 1f;

	public float rotation;

	public float randomRotation;

	[FoldoutGroup("Color", 0)]
	public Color color = Color.magenta;

	[FoldoutGroup("Color", 0)]
	public Color randomColor = Color.magenta;

	[FoldoutGroup("Color", 0)]
	public Color randomAddedColor = Color.black;

	[FoldoutGroup("Color", 0)]
	public float randomAddedSaturation;

	[FoldoutGroup("Color", 0)]
	public bool singleRandomValueColor = true;

	public AnimationCurve alphaOverTime = AnimationCurve.Linear(0f, 1f, 1f, 1f);
}
