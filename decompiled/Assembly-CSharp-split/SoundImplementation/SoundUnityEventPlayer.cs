using Sonigon;
using UnityEngine;

namespace SoundImplementation;

public class SoundUnityEventPlayer : MonoBehaviour
{
	public bool triggerStartOnStart;

	public bool triggerStartOnEnable;

	public bool triggerEndOnDestroy;

	public bool triggerEndOnDisable;

	public bool useLocalTransformForInstanceID;

	public SoundEvent soundStart;

	public SoundEvent soundStartLoop;

	public SoundEvent soundEnd;

	private bool soundStartLoopPlaying;

	private bool initialized;

	private Transform transformPosition;

	private void Initialize()
	{
		if (!initialized)
		{
			initialized = true;
			transformPosition = base.transform;
		}
	}

	private Transform GetTransformInstanceID()
	{
		if (useLocalTransformForInstanceID)
		{
			return transformPosition;
		}
		return SoundManager.Instance.GetTransform();
	}

	private void Start()
	{
		if (triggerStartOnStart)
		{
			PlayStart();
		}
	}

	private void OnEnable()
	{
		if (triggerStartOnEnable)
		{
			PlayStart();
		}
	}

	public void PlayStart()
	{
		Initialize();
		if (soundStart != null)
		{
			SoundManager.Instance.PlayAtPosition(soundStart, GetTransformInstanceID(), transformPosition);
		}
		if (soundStartLoop != null)
		{
			soundStartLoopPlaying = true;
			SoundManager.Instance.PlayAtPosition(soundStartLoop, GetTransformInstanceID(), transformPosition);
		}
	}

	private void OnDestroy()
	{
		if (triggerEndOnDestroy)
		{
			PlayEnd();
		}
	}

	private void OnDisable()
	{
		if (triggerEndOnDisable)
		{
			PlayEnd();
		}
	}

	public void PlayEnd()
	{
		Initialize();
		if (soundEnd != null)
		{
			SoundManager.Instance.PlayAtPosition(soundEnd, GetTransformInstanceID(), transformPosition);
		}
		if (soundStartLoopPlaying && soundStartLoop != null)
		{
			soundStartLoopPlaying = false;
			SoundManager.Instance.StopAtPosition(soundStartLoop, transformPosition);
		}
	}
}
