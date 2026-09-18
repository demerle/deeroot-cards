using UnityEngine;

public class SetSpriteColor : MonoBehaviour
{
	private SpriteRenderer sprite;

	public Color[] cols;

	private void Start()
	{
		sprite = GetComponent<SpriteRenderer>();
	}

	public void SetColor(int id)
	{
		sprite.color = cols[id];
	}
}
