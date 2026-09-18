using System;
using Sonigon;
using UnityEngine;

namespace SoundImplementation
{
	[Serializable]
	[CreateAssetMenu(fileName = "_SoundImpactModifier", menuName = "Sound Implementation/Sound Impact Modifier", order = 1)]
	public class SoundImpactModifier : ScriptableObject
	{
		[Header("Sound Priority")]
		public int priority;

		[Header("Sound Events")]
		public SoundEvent impactCharacter;

		public SoundEvent impactEnvironment;
	}
}
