using UnityEngine;

public class NextArt : MonoBehaviour
{
	private void Start()
	{
		ArtHandler.instance.NextArt();
	}
}
