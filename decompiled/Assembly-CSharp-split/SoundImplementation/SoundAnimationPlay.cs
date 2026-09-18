using System;
using Sonigon;

namespace SoundImplementation;

[Serializable]
public class SoundAnimationPlay
{
	public SoundEvent soundEvent;

	public CurveAnimationUse curveAnimationUse;

	public float soundDelay;

	[NonSerialized]
	public bool soundHasPlayed;
}
