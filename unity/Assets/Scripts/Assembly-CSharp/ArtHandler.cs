using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class ArtHandler : MonoBehaviour
{
	public ArtInstance[] arts;

	public PostProcessVolume volume;

	public static ArtHandler instance;

	private ColorGrading colorGrading;

	private int currentArt = -1;

	private void Awake()
	{
		instance = this;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.LeftShift))
		{
			NextArt();
		}
	}

	public void NextArt()
	{
		for (int i = 0; i < arts.Length; i++)
		{
			arts[i].TogglePart(false);
		}
		int num = Random.Range(0, arts.Length);
		if (num >= arts.Length)
		{
			num = 0;
		}
		ApplyArt(arts[num]);
	}

	private void ApplyArt(ArtInstance art)
	{
		art.TogglePart(true);
		currentArt = GetArtID(art);
		volume.profile = art.profile;
	}

	public void SetSpecificArt(ArtInstance art)
	{
		ApplyArt(art);
	}

	public void ApplyPost(PostProcessProfile profileToSet)
	{
		volume.profile = profileToSet;
	}

	public void SetSpecificArt(string artName)
	{
		for (int i = 0; i < arts.Length; i++)
		{
			if (arts[i].profile.name == artName)
			{
				ApplyArt(arts[i]);
				break;
			}
		}
	}

	private int GetArtID(ArtInstance art)
	{
		int result = -1;
		for (int i = 0; i < arts.Length; i++)
		{
			if (art == arts[i])
			{
				result = i;
			}
		}
		return result;
	}
}
