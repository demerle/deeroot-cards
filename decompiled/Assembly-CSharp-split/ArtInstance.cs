using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
public class ArtInstance
{
	[FoldoutGroup("$profile", 0)]
	public PostProcessProfile profile;

	[FoldoutGroup("$profile", 0)]
	public ParticleSystem[] parts;

	public void TogglePart(bool on)
	{
		for (int i = 0; i < parts.Length; i++)
		{
			parts[i].gameObject.SetActive(on);
			parts[i].Play();
		}
	}
}
