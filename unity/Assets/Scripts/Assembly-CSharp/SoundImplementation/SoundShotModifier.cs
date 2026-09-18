using System;
using Sonigon;
using UnityEngine;

namespace SoundImplementation
{
	[Serializable]
	[CreateAssetMenu(fileName = "_SoundShotModifier", menuName = "Sound Implementation/Sound Shot Modifier", order = 0)]
	public class SoundShotModifier : ScriptableObject
	{
		[Header("Sound Priority")]
		public int priority;

		[Header("Sound Events")]
		public SoundEvent single;

		public SoundEvent singleAutoLoop;

		public SoundEvent singleAutoTail;

		public SoundEvent shotgun;

		public SoundEvent shotgunAutoLoop;

		public SoundEvent shotgunAutoTail;
	}
}
