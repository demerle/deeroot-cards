using Sonigon;
using UnityEngine;

namespace SoundImplementation;

public class SoundPlayerStatic : MonoBehaviour
{
	[SerializeField]
	private SoundEvent soundButtonHover;

	[SerializeField]
	private SoundEvent soundButtonClick;

	[SerializeField]
	private SoundEvent soundMatchFound;

	[SerializeField]
	private SoundEvent soundPlayerAdded;

	[SerializeField]
	private SoundEvent soundPlayerBallAppear;

	[SerializeField]
	private SoundEvent soundPlayerBallDisappear;

	[SerializeField]
	private SoundEvent soundLevelTransitionIn;

	[SerializeField]
	private SoundEvent soundLevelTransitionOut;

	private bool isGoingToDelete;

	private bool useDontDestroyOnLoad = true;

	private bool debugInstanceDestroyed = true;

	public static SoundPlayerStatic Instance { get; private set; }

	public void PlayButtonHover()
	{
		SoundManager.Instance.Play(soundButtonHover, base.transform);
	}

	public void PlayButtonClick()
	{
		SoundManager.Instance.Play(soundButtonClick, base.transform);
	}

	public void PlayMatchFound()
	{
		SoundManager.Instance.Play(soundMatchFound, base.transform);
	}

	public void PlayPlayerAdded()
	{
		SoundManager.Instance.Play(soundPlayerAdded, base.transform);
	}

	public void PlayPlayerBallAppear()
	{
		SoundManager.Instance.Play(soundPlayerBallAppear, base.transform);
	}

	public void PlayPlayerBallDisappear()
	{
		SoundManager.Instance.Play(soundPlayerBallDisappear, base.transform);
	}

	public void PlayLevelTransitionIn()
	{
		SoundManager.Instance.Play(soundLevelTransitionIn, base.transform);
	}

	public void PlayLevelTransitionOut()
	{
		SoundManager.Instance.Play(soundLevelTransitionOut, base.transform);
	}

	private void Awake()
	{
		InstanceCheck();
		if (useDontDestroyOnLoad && !isGoingToDelete)
		{
			Object.DontDestroyOnLoad(base.gameObject);
		}
	}

	private void InstanceCheck()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			isGoingToDelete = true;
			if (Application.isPlaying)
			{
				Object.Destroy(base.gameObject);
			}
		}
	}
}
