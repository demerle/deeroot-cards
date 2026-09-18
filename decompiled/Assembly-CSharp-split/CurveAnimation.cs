using System.Collections;
using Sirenix.OdinInspector;
using Sonigon;
using SoundImplementation;
using UnityEngine;
using UnityEngine.Events;

public class CurveAnimation : MonoBehaviour
{
	[Header("Sound")]
	public SoundAnimationPlay[] soundPlay = new SoundAnimationPlay[0];

	[Header("Settings")]
	public CurveAnimationInstance[] animations;

	[HideInInspector]
	public Vector3 startScale;

	[HideInInspector]
	public Vector3 startLocalPos;

	[HideInInspector]
	public Vector3 startAnchoredPos;

	[HideInInspector]
	public Vector3 startRotation;

	public CurveAnimationUse currentState = CurveAnimationUse.Out;

	private RectTransform rectTransform;

	[FoldoutGroup("Global settings", 0)]
	public bool useTimeScale = true;

	[FoldoutGroup("Global settings", 0)]
	public bool X = true;

	[FoldoutGroup("Global settings", 0)]
	public bool Y = true;

	[FoldoutGroup("Global settings", 0)]
	public bool Z = true;

	[FoldoutGroup("Global settings", 0)]
	public bool stopAllAnimations;

	private bool inited;

	private void Init()
	{
		if (!inited)
		{
			startScale = base.transform.localScale;
			startLocalPos = base.transform.localScale;
			rectTransform = GetComponent<RectTransform>();
			if ((bool)rectTransform)
			{
				startAnchoredPos = rectTransform.anchoredPosition;
			}
			startRotation = base.transform.localEulerAngles;
		}
	}

	private void Awake()
	{
		Init();
	}

	private void OnDisable()
	{
		StopAllCoroutines();
		ResetAnimationState();
		for (int i = 0; i < animations.Length; i++)
		{
			animations[i].isPlaying = false;
		}
	}

	private void OnEnable()
	{
		ResetAnimationState();
		for (int i = 0; i < animations.Length; i++)
		{
			if (animations[i].playOnAwake)
			{
				PlayAnimation(animations[i]);
			}
		}
	}

	[Button]
	public void PlayIn()
	{
		PlayAnimationWithUse(CurveAnimationUse.In);
	}

	[Button]
	public void PlayOut()
	{
		PlayAnimationWithUse(CurveAnimationUse.Out);
	}

	[Button]
	public void PlayBoop()
	{
		PlayAnimationWithUse(CurveAnimationUse.Boop);
	}

	[Button]
	public void Stop()
	{
		StopAllCoroutines();
	}

	private void ResetAnimationState()
	{
		ApplyAnimationFrame(GetAnimationWithUse(CurveAnimationUse.In), 0f);
	}

	private CurveAnimationInstance GetAnimationWithUse(CurveAnimationUse use)
	{
		for (int i = 0; i < animations.Length; i++)
		{
			if (animations[i].animationUse == use)
			{
				return animations[i];
			}
		}
		return animations[0];
	}

	public void PlayAnimation(CurveAnimationInstance animation)
	{
		for (int i = 0; i < soundPlay.Length; i++)
		{
			if (soundPlay[i] != null)
			{
				soundPlay[i].soundHasPlayed = false;
			}
		}
		if (stopAllAnimations)
		{
			StopAllCoroutines();
		}
		if (animation.isPlaying && animation.animation != null)
		{
			StopCoroutine(animation.animation);
		}
		animation.animation = StartCoroutine(DoAnimation(animation));
	}

	public void PlayAnimationWithUse(CurveAnimationUse animationUse)
	{
		for (int i = 0; i < soundPlay.Length; i++)
		{
			if (soundPlay[i] != null)
			{
				soundPlay[i].soundHasPlayed = false;
			}
		}
		if (stopAllAnimations)
		{
			StopAllCoroutines();
		}
		currentState = animationUse;
		for (int j = 0; j < animations.Length; j++)
		{
			if (animations[j].animationUse == animationUse)
			{
				if (animations[j].isPlaying && animations[j].animation != null)
				{
					StopCoroutine(animations[j].animation);
				}
				animations[j].animation = StartCoroutine(DoAnimation(animations[j]));
			}
		}
	}

	private IEnumerator DoAnimation(CurveAnimationInstance animation)
	{
		StartCoroutine(DelayEvent(animation.delay / animation.speed, animation.delayedEvent));
		animation.statEvent.Invoke();
		animation.isPlaying = true;
		float c = 0f;
		float t = animation.Curve().keys[animation.Curve().keys.Length - 1].time;
		while (c < t)
		{
			c += (useTimeScale ? (TimeHandler.deltaTime * animation.speed) : (Time.unscaledDeltaTime * animation.speed));
			for (int i = 0; i < soundPlay.Length; i++)
			{
				if (soundPlay[i] != null && soundPlay[i].soundEvent != null && c >= soundPlay[i].soundDelay && animation.animationUse == soundPlay[i].curveAnimationUse && !soundPlay[i].soundHasPlayed && (bool)SoundManager.Instance)
				{
					soundPlay[i].soundHasPlayed = true;
					SoundManager.Instance.Play(soundPlay[i].soundEvent, base.transform);
				}
			}
			ApplyAnimationFrame(animation, c);
			yield return null;
		}
		ApplyAnimationFrame(animation, t);
		animation.isPlaying = false;
		animation.endEvent.Invoke();
		if (animation.loop)
		{
			PlayAnimationWithUse(animation.animationUse);
		}
	}

	private IEnumerator DelayEvent(float seconds, UnityEvent eventToCall)
	{
		yield return new WaitForSeconds(seconds);
		eventToCall.Invoke();
	}

	private void ApplyAnimationFrame(CurveAnimationInstance anim, float time)
	{
		if (anim.animationType == CurveAnimationType.Scale)
		{
			Vector3 vector = startScale * anim.Curve().Evaluate(time) * anim.multiplier;
			Vector3 localScale = new Vector3(X ? vector.x : base.transform.localScale.x, Y ? vector.y : base.transform.localScale.y, Z ? vector.z : base.transform.localScale.z);
			base.transform.localScale = localScale;
		}
		else if (anim.animationType == CurveAnimationType.Position)
		{
			base.transform.localPosition = startLocalPos + anim.animDirection * anim.Curve().Evaluate(time) * anim.multiplier;
		}
		else if (anim.animationType == CurveAnimationType.RectPosition)
		{
			rectTransform.anchoredPosition = startAnchoredPos + anim.animDirection * anim.Curve().Evaluate(time) * anim.multiplier;
		}
		else if (anim.animationType == CurveAnimationType.Rotation)
		{
			base.transform.localEulerAngles = startRotation + anim.animDirection * anim.Curve().Evaluate(time) * anim.multiplier;
		}
	}

	public bool IsPlaying()
	{
		bool result = false;
		for (int i = 0; i < animations.Length; i++)
		{
			if (animations[i].isPlaying)
			{
				result = true;
			}
		}
		return result;
	}
}
