using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class SetSpecificArt : MonoBehaviour
{
	public PostProcessProfile profile;

	public string artName;

	public bool playOnStart;

	private void Start()
	{
		if (playOnStart)
		{
			if ((bool)profile)
			{
				ArtHandler.instance.ApplyPost(profile);
			}
			if (artName != "")
			{
				ArtHandler.instance.SetSpecificArt(artName);
			}
		}
	}

	public void Go()
	{
		if ((bool)profile)
		{
			ArtHandler.instance.ApplyPost(profile);
		}
		if (artName != "")
		{
			ArtHandler.instance.SetSpecificArt(artName);
		}
	}
}
