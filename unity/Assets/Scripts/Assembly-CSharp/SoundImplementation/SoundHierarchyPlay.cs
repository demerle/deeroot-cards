using System;
using Sonigon;
using UnityEngine;

namespace SoundImplementation
{
	public class SoundHierarchyPlay : MonoBehaviour
	{
		[NonSerialized]
		public Transform instanceIDTransform;

		[NonSerialized]
		public SoundHierarchySpawn soundHierarchySpawn;

		[NonSerialized]
		public SoundHierarchyDepth soundHierarchyDepth;

		private SoundParameterIntensity soundParameterIntensityParentContinious = new SoundParameterIntensity(1f, UpdateMode.Continuous);

		private SoundParameterIntensity soundParameterIntensityChildOnce = new SoundParameterIntensity(0f);

		private Vector3 lastPosition;

		private Transform transformPosition;

		private bool bulletHasTriggered;

		private bool stopOnDisablePlaying;

		private Transform GetCurrentInstanceIDTransform()
		{
			if (soundHierarchySpawn.soundPolyGrouping == SoundPolyGrouping.global)
			{
				return SoundManager.Instance.GetTransform();
			}
			return instanceIDTransform;
		}

		private void FixedUpdate()
		{
			if (soundHierarchyDepth == SoundHierarchyDepth.bulletParent && soundHierarchySpawn.soundParentVelocityToIntensity && bulletHasTriggered && soundHierarchySpawn != null)
			{
				soundParameterIntensityParentContinious.intensity = Vector3.Distance(lastPosition, transformPosition.position);
				lastPosition = transformPosition.position;
			}
		}

		private void OnDisable()
		{
			if (bulletHasTriggered && stopOnDisablePlaying)
			{
				stopOnDisablePlaying = false;
				SoundManager.Instance.StopAtPosition(soundHierarchySpawn.soundChildChildStopOnDisable, transformPosition);
			}
		}

		public void PlayBullet()
		{
			transformPosition = base.transform;
			bulletHasTriggered = true;
			lastPosition = transformPosition.position;
			if (soundHierarchyDepth == SoundHierarchyDepth.bulletParent)
			{
				if (soundHierarchySpawn.soundParent != null)
				{
					if (soundHierarchySpawn.soundParentVelocityToIntensity)
					{
						SoundManager.Instance.PlayAtPosition(soundHierarchySpawn.soundParent, GetCurrentInstanceIDTransform(), transformPosition, soundParameterIntensityParentContinious);
					}
					else
					{
						SoundManager.Instance.PlayAtPosition(soundHierarchySpawn.soundParent, GetCurrentInstanceIDTransform(), transformPosition);
					}
				}
				if (soundHierarchySpawn.soundParentStopOnDisable != null)
				{
					stopOnDisablePlaying = true;
					SoundManager.Instance.PlayAtPosition(soundHierarchySpawn.soundParentStopOnDisable, GetCurrentInstanceIDTransform(), transformPosition);
				}
			}
			else if (soundHierarchyDepth == SoundHierarchyDepth.bulletChild && soundHierarchySpawn.soundChild != null)
			{
				if (soundHierarchySpawn.soundChildVelocityToIntensity)
				{
					SoundManager.Instance.PlayAtPosition(soundHierarchySpawn.soundChild, GetCurrentInstanceIDTransform(), transformPosition, soundParameterIntensityChildOnce);
				}
				else
				{
					SoundManager.Instance.PlayAtPosition(soundHierarchySpawn.soundChild, GetCurrentInstanceIDTransform(), transformPosition);
				}
			}
			else if (soundHierarchyDepth == SoundHierarchyDepth.bulletChildChild)
			{
				if (soundHierarchySpawn.soundChildChild != null)
				{
					SoundManager.Instance.PlayAtPosition(soundHierarchySpawn.soundChildChild, GetCurrentInstanceIDTransform(), transformPosition);
				}
				if (soundHierarchySpawn.soundChildChildStopOnDisable != null)
				{
					stopOnDisablePlaying = true;
					SoundManager.Instance.PlayAtPosition(soundHierarchySpawn.soundChildChildStopOnDisable, GetCurrentInstanceIDTransform(), transformPosition);
				}
			}
		}
	}
}
