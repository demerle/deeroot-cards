using UnityEngine;

public class GetColor : MonoBehaviour
{
	public ColorHandler.ColorType colorType;

	private bool inited;

	public void Start()
	{
		if (!inited)
		{
			inited = true;
			SpriteRenderer component = GetComponent<SpriteRenderer>();
			if ((bool)component)
			{
				component.color = ColorHandler.instance.GetColor(colorType);
			}
		}
	}
}
