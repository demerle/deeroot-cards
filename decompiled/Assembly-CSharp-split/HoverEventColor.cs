using UnityEngine;
using UnityEngine.UI;

public class HoverEventColor : MonoBehaviour
{
	public Color hoverColor;

	private Color defaultColor;

	private HoverEvent hover;

	private Image img;

	private void Start()
	{
		img = GetComponent<Image>();
		defaultColor = img.color;
		hover = GetComponent<HoverEvent>();
		hover.enterEvent.AddListener(Enter);
		hover.exitEvent.AddListener(Exit);
	}

	private void Enter()
	{
		img.color = hoverColor;
	}

	private void Exit()
	{
		img.color = defaultColor;
	}
}
