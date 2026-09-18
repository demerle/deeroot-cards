using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

public class LineEffect : MonoBehaviour
{
	public enum LineType
	{
		Line = 0,
		Ring = 1
	}

	public enum AnimType
	{
		Width = 0,
		Offset = 1
	}

	public bool playOnAwake;

	public bool loop;

	public LineType lineType;

	public int segments = 20;

	public float globalTimeSpeed = 1f;

	public bool raycastCollision;

	[FoldoutGroup("Animation", 0)]
	public AnimationCurve lineWidthOverTimeCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

	[Space(10f)]
	[FoldoutGroup("Animation", 0)]
	public bool useColorOverTime;

	[FoldoutGroup("Animation", 0)]
	[ShowIf("useColorOverTime", true)]
	public Gradient colorOverTime;

	[Space(10f)]
	[FoldoutGroup("Animation", 0)]
	[ShowIf("lineType", LineType.Ring, true)]
	public float radius = 5f;

	[FoldoutGroup("Animation", 0)]
	[ShowIf("lineType", LineType.Ring, true)]
	public AnimationCurve radiusOverTime = AnimationCurve.Linear(0f, 1f, 1f, 1f);

	[Space(10f)]
	[FoldoutGroup("Special", 0)]
	public float inheritScaleFactor;

	[Space(10f)]
	public LineEffectInstance[] effects;

	private LineRenderer line;

	[HideInInspector]
	public float counter;

	private float startWidth;

	[HideInInspector]
	public float currentWidth;

	[FoldoutGroup("Debug", 0)]
	public bool debug;

	[FoldoutGroup("Debug", 0)]
	public Transform fromPos;

	[FoldoutGroup("Debug", 0)]
	public Transform toPos;

	[FoldoutGroup("Debug", 0)]
	public Transform bezierPos;

	[HideInInspector]
	public float offsetMultiplier = 1f;

	[HideInInspector]
	public float widthMultiplier = 1f;

	private float scaleMultiplier = 1f;

	private bool inited;

	public bool isPlaying;

	private Coroutine widthAnim;

	private Coroutine offsetAnim;

	private void Start()
	{
		globalTimeSpeed *= UnityEngine.Random.Range(0.95f, 1.05f);
	}

	private void OnEnable()
	{
		if (!inited)
		{
			Init();
		}
		if (playOnAwake && lineType == LineType.Ring)
		{
			Play(base.transform);
		}
	}

	private void Init()
	{
		if (!inited)
		{
			line = GetComponent<LineRenderer>();
			line.positionCount = segments;
			line.useWorldSpace = true;
			startWidth = line.widthMultiplier;
			inited = true;
			scaleMultiplier = 1f - inheritScaleFactor + base.transform.lossyScale.x * inheritScaleFactor;
		}
	}

	private void Update()
	{
		if (!debug)
		{
			return;
		}
		if (lineType == LineType.Line && (bool)fromPos && (bool)toPos)
		{
			if ((bool)bezierPos)
			{
				DrawLine(fromPos.position, toPos.position, bezierPos.position);
			}
			else
			{
				DrawLine(fromPos.position, toPos.position);
			}
		}
		if (lineType == LineType.Ring && (bool)fromPos)
		{
			DrawLine(fromPos.position, Vector3.zero);
		}
	}

	public void StartDraw()
	{
		if (!base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(true);
		}
		counter = 0f;
	}

	public void DrawLine(Vector3 start, Vector3 end)
	{
		DrawLine(start, end, Vector3.one * 100f);
	}

	public void DrawLine(Vector3 start, Vector3 end, Vector3 bezier)
	{
		Init();
		Vector3 vector = Vector3.zero;
		for (int i = 0; i < line.positionCount; i++)
		{
			float num = 0f;
			for (int j = 0; j < effects.Length; j++)
			{
				if (!effects[j].active)
				{
					continue;
				}
				float num2 = 0f;
				float num3 = (float)i / ((float)line.positionCount - 1f);
				float time = num3;
				float num4 = effects[j].mainCurveTiling;
				if (effects[j].tilingPerMeter)
				{
					if (lineType == LineType.Line)
					{
						num4 *= (start - end).magnitude;
					}
					if (lineType == LineType.Ring)
					{
						num4 *= radius;
					}
				}
				num3 *= num4;
				if (effects[j].mainCurveScrollSpeed > 0f)
				{
					num3 += effects[j].mainCurveScrollSpeed * Time.unscaledTime;
				}
				if (effects[j].mainCurveScrollSpeed < 0f)
				{
					num3 += (0f - effects[j].mainCurveScrollSpeed) * (100000f - Time.unscaledTime);
				}
				num3 %= 1f;
				num2 = effects[j].mainCurve.Evaluate(num3);
				num2 *= effects[j].mainCurveMultiplier;
				num2 *= effects[j].effectOverLineCurve.Evaluate(time);
				num2 *= effects[j].effectOverTimeCurve.Evaluate(counter);
				if (effects[j].curveType == LineEffectInstance.CurveType.Add)
				{
					num += num2;
				}
				else if (effects[j].curveType == LineEffectInstance.CurveType.Multiply)
				{
					num *= num2;
				}
			}
			float t = (float)i / ((float)line.positionCount - 1f);
			Vector3 vector2 = Vector3.zero;
			Vector3 vector3 = Vector3.zero;
			if (lineType == LineType.Ring)
			{
				float f = (float)Math.PI / 180f * ((float)i * 360f / (float)(segments - 1));
				vector2 = start + new Vector3(Mathf.Sin(f) * (radius * base.transform.root.localScale.x * radiusOverTime.Evaluate(counter)), Mathf.Cos(f) * (radius * base.transform.root.localScale.x * radiusOverTime.Evaluate(counter)), 0f);
				vector3 = ((!(vector != Vector3.zero)) ? Vector3.up : Vector3.Cross(Vector3.forward, vector2 - vector).normalized);
			}
			if (lineType == LineType.Line)
			{
				vector2 = Vector3.Lerp(start, end, t);
				vector3 = Vector3.Cross(start - end, Vector3.forward).normalized;
				if (bezier != Vector3.one * 100f)
				{
					vector2 = BezierCurve.QuadraticBezier(start, bezier, end, t);
				}
			}
			vector = vector2;
			line.SetPosition(i, vector2 + vector3 * num * offsetMultiplier);
		}
		if (raycastCollision)
		{
			RaycastPositions();
		}
		if (lineType == LineType.Ring)
		{
			SmoothSeam();
		}
		line.widthMultiplier = lineWidthOverTimeCurve.Evaluate(counter) * startWidth * scaleMultiplier * widthMultiplier;
		currentWidth = line.widthMultiplier;
		if (useColorOverTime)
		{
			line.startColor = colorOverTime.Evaluate(counter);
			line.endColor = colorOverTime.Evaluate(counter);
		}
		counter += Time.unscaledDeltaTime * globalTimeSpeed;
		if (counter > 1f)
		{
			if (debug || loop)
			{
				StartDraw();
			}
			else
			{
				base.gameObject.SetActive(false);
			}
		}
	}

	private void RaycastPositions()
	{
		for (int i = 0; i < line.positionCount; i++)
		{
			line.SetPosition(i, PhysicsFunctions.ObstructionPoint(base.transform.position, line.GetPosition(i)));
		}
	}

	private void SmoothSeam()
	{
		float num = 0.1f;
		Vector3 b = (line.GetPosition(0) + line.GetPosition(line.positionCount - 1)) * 0.5f;
		for (int i = 0; i < line.positionCount; i++)
		{
			float num2 = (float)i / ((float)line.positionCount - 1f);
			Vector3 zero2 = Vector3.zero;
			Vector3 zero = Vector3.zero;
			Vector3 position = line.GetPosition(i);
			float t;
			if (num2 > 0.5f)
			{
				t = Mathf.Clamp((num2 - (1f - num)) / num, 0f, 1f);
			}
			else
			{
				t = Mathf.Clamp(num2 * (1f / num), 0f, 1f);
				t = 1f - t;
			}
			zero = Vector3.Lerp(position, b, t) - position;
			zero.x *= 0.5f;
			line.SetPosition(i, position + zero);
		}
		line.SetPosition(1, new Vector3(line.GetPosition(1).x, b.y));
		line.SetPosition(line.positionCount - 2, new Vector3(line.GetPosition(line.positionCount - 2).x, b.y));
	}

	public void Stop()
	{
		StopAllCoroutines();
		if (!loop)
		{
			base.gameObject.SetActive(false);
		}
	}

	public void Play()
	{
		Play(base.transform);
	}

	private void OnDisable()
	{
		Stop();
	}

	public void ResetMultipliers()
	{
		offsetMultiplier = 1f;
		scaleMultiplier = 1f;
		widthMultiplier = 1f;
	}

	public void Play(Transform fromTransform, Transform toTransform, float bezierOffset = 0f)
	{
		StartDraw();
		StopAllCoroutines();
		StartCoroutine(DoPlay(fromTransform, toTransform, bezierOffset));
	}

	private IEnumerator DoPlay(Transform fromTransform, Transform toTransform, float bezierOffset = 0f)
	{
		isPlaying = true;
		Vector3 currentBez = Vector3.zero;
		while (base.gameObject.activeSelf && (bool)fromTransform && (bool)toTransform)
		{
			if (bezierOffset != 0f)
			{
				float num = 1f;
				if (toTransform.position.x < fromTransform.position.x)
				{
					num = -1f;
				}
				Vector3 vector = Vector3.Cross(Vector3.forward, toTransform.position - fromTransform.position).normalized * bezierOffset * num;
				currentBez = ((!(currentBez == Vector3.zero)) ? Vector3.Lerp(currentBez, vector, TimeHandler.deltaTime * 5f) : vector);
				Vector3 bezier = currentBez + (fromTransform.position + toTransform.position) * 0.5f;
				DrawLine(fromTransform.position, toTransform.position, bezier);
			}
			else
			{
				DrawLine(fromTransform.position, toTransform.position);
			}
			yield return null;
		}
		isPlaying = false;
	}

	public void Play(Transform fromTransform, Vector3 toPosition, float bezierOffset = 0f)
	{
		StartDraw();
		StopAllCoroutines();
		StartCoroutine(DoPlay(fromTransform, toPosition, bezierOffset));
	}

	private IEnumerator DoPlay(Transform fromTransform, Vector3 toPosition, float bezierOffset = 0f)
	{
		Vector3 currentBezOffset = Vector3.zero;
		while (base.gameObject.activeSelf && (bool)fromTransform)
		{
			if (bezierOffset != 0f)
			{
				float num = 1f;
				if (toPosition.x < fromTransform.position.x)
				{
					num = -1f;
				}
				Vector3 vector = Vector3.Cross(Vector3.forward, toPosition - fromTransform.position).normalized * bezierOffset * num;
				currentBezOffset = ((!(currentBezOffset == Vector3.zero)) ? Vector3.Lerp(currentBezOffset, vector, TimeHandler.deltaTime * 2f) : vector);
				Vector3 vector2 = currentBezOffset + (fromTransform.position + toPosition) * 0.5f;
				Vector3 bezier = (fromTransform.position + toPosition) * 0.5f;
				DrawLine(fromTransform.position, toPosition, bezier);
			}
			else
			{
				DrawLine(fromTransform.position, toPosition);
			}
			yield return null;
		}
	}

	public void Play(Transform fromTransform)
	{
		StartDraw();
		StopAllCoroutines();
		StartCoroutine(DoPlay(fromTransform));
	}

	private IEnumerator DoPlay(Transform fromTransform)
	{
		while (base.gameObject.activeSelf && (bool)fromTransform)
		{
			DrawLine(fromTransform.position, Vector3.zero);
			yield return null;
		}
	}

	public void PlayAnim(AnimType animType, AnimationCurve curve, float speed = 1f)
	{
		if (animType == AnimType.Offset && offsetAnim != null)
		{
			StopCoroutine(offsetAnim);
		}
		if (animType == AnimType.Width && widthAnim != null)
		{
			StopCoroutine(widthAnim);
		}
		Coroutine coroutine = StartCoroutine(DoPlayAnim(animType, curve, speed));
		if (animType == AnimType.Offset)
		{
			offsetAnim = coroutine;
		}
		if (animType == AnimType.Width)
		{
			widthAnim = coroutine;
		}
	}

	private IEnumerator DoPlayAnim(AnimType animType, AnimationCurve curve, float speed = 1f)
	{
		float c = 0f;
		float t = curve.keys[curve.keys.Length - 1].time;
		while (c < t)
		{
			if (animType == AnimType.Offset)
			{
				offsetMultiplier = curve.Evaluate(c);
			}
			if (animType == AnimType.Width)
			{
				widthMultiplier = curve.Evaluate(c);
			}
			c += TimeHandler.deltaTime * speed;
			yield return null;
		}
	}

	internal float GetRadius()
	{
		return radius * base.transform.root.localScale.x * radiusOverTime.Evaluate(counter);
	}
}
