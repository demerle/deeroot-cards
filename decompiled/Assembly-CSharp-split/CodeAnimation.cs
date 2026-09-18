using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class CodeAnimation : MonoBehaviour
{
	[HideInInspector]
	public bool isPlaying;

	public bool loonIn;

	public bool playInOnAwake;

	public bool playInOnEnable;

	public float enablePlayDelay;

	public bool interuptAnimations = true;

	public bool setFirstFrame;

	public bool useTimeScale = true;

	private Vector3 defaultScale;

	private Vector3 defaultLocalPosition;

	private Vector3 defaultRectPosition;

	private Vector3 defaultLocalRotation;

	public CodeAnimationInstance[] animations;

	private RectTransform rectTransform;

	[HideInInspector]
	public CodeAnimationInstance.AnimationUse currentState;

	[HideInInspector]
	public float animationValue;

	private Action AnimationChangeAction;

	public void Start()
	{
		currentState = CodeAnimationInstance.AnimationUse.None;
		rectTransform = GetComponent<RectTransform>();
		for (int i = 0; i < animations.Length; i++)
		{
			animations[i].animationSpeed += UnityEngine.Random.Range((0f - animations[i].animationSpeed) * animations[i].randomSpeedAmount, animations[i].animationSpeed * animations[i].randomSpeedAmount);
			if (animations[i].animationUse == CodeAnimationInstance.AnimationUse.In)
			{
				float num = animations[i].curve[animations[i].curve.length - 1].value - animations[i].curve[0].value;
				if ((bool)rectTransform && animations[i].animationType == CodeAnimationInstance.AnimationType.rectPosition)
				{
					rectTransform.anchoredPosition += num * (Vector2)animations[i].direction * (0f - animations[i].multiplier);
				}
			}
		}
		SetDefaults();
		if (playInOnAwake)
		{
			PlayIn();
		}
		if (setFirstFrame)
		{
			ApplyValues(animations[0], 0f);
		}
	}

	private void OnEnable()
	{
		if (playInOnEnable)
		{
			StartCoroutine(DelayEnablePlay());
		}
	}

	private IEnumerator DelayEnablePlay()
	{
		yield return new WaitForSecondsRealtime(enablePlayDelay);
		PlayIn();
	}

	private void SetDefaults()
	{
		defaultScale = base.transform.localScale;
		defaultLocalPosition = base.transform.localPosition;
		defaultLocalRotation = base.transform.localRotation.eulerAngles;
		if ((bool)rectTransform)
		{
			defaultRectPosition = rectTransform.anchoredPosition;
		}
	}

	public void Animate(CodeAnimationInstance.AnimationUse use)
	{
		currentState = use;
		if (interuptAnimations)
		{
			StopAllCoroutines();
		}
		for (int i = 0; i < animations.Length; i++)
		{
			if (animations[i].animationUse == use)
			{
				StartCoroutine(PlayAnimations(animations[i]));
			}
		}
	}

	[Button]
	public void PlayIn()
	{
		Animate(CodeAnimationInstance.AnimationUse.In);
	}

	[Button]
	public void PlayOut()
	{
		Animate(CodeAnimationInstance.AnimationUse.Out);
	}

	[Button]
	public void PlayBoop()
	{
		Animate(CodeAnimationInstance.AnimationUse.Boop);
	}

	private IEnumerator PlayAnimations(CodeAnimationInstance animation)
	{
		isPlaying = true;
		animation.startEvent.Invoke();
		StartCoroutine(DelayTimedEvent(animation.eventTiming / animation.animationSpeed, animation.timedEvent));
		float t = animation.curve.keys[animation.curve.keys.Length - 1].time;
		float c = 0f;
		while (c < t)
		{
			ApplyValues(animation, c);
			c += (useTimeScale ? TimeHandler.deltaTime : Time.unscaledDeltaTime) * animation.animationSpeed;
			yield return null;
		}
		ApplyValues(animation, t);
		isPlaying = false;
		animation.endEvent.Invoke();
		if (loonIn)
		{
			PlayIn();
		}
	}

	private void ApplyValues(CodeAnimationInstance animation, float time)
	{
		if (animation.animationType == CodeAnimationInstance.AnimationType.rectPosition && (bool)rectTransform)
		{
			rectTransform.anchoredPosition = defaultRectPosition + animation.direction * animation.curve.Evaluate(time) * animation.multiplier;
		}
		if (animation.animationType == CodeAnimationInstance.AnimationType.position)
		{
			base.transform.localPosition = defaultLocalPosition + animation.direction * animation.curve.Evaluate(time) * animation.multiplier;
		}
		if (animation.animationType == CodeAnimationInstance.AnimationType.scale)
		{
			Vector3 localScale = defaultScale * animation.curve.Evaluate(time) * animation.multiplier;
			if (!animation.X)
			{
				localScale.x = base.transform.localScale.x;
			}
			if (!animation.Y)
			{
				localScale.y = base.transform.localScale.y;
			}
			if (!animation.Z)
			{
				localScale.z = base.transform.localScale.z;
			}
			base.transform.localScale = localScale;
		}
		if (animation.animationType == CodeAnimationInstance.AnimationType.floatNumber)
		{
			animationValue = animation.curve.Evaluate(time) * animation.multiplier;
		}
		if (animation.animationType == CodeAnimationInstance.AnimationType.rotation)
		{
			base.transform.localRotation = Quaternion.Euler(animation.curve.Evaluate(time) * animation.direction + defaultLocalRotation);
		}
		if (AnimationChangeAction != null)
		{
			AnimationChangeAction();
		}
	}

	public void AddAnimationChangeAction(Action action)
	{
		AnimationChangeAction = (Action)Delegate.Combine(AnimationChangeAction, action);
	}

	private IEnumerator DelayTimedEvent(float time, UnityEvent eventToCall)
	{
		yield return new WaitForSeconds(time);
		eventToCall.Invoke();
	}
}
