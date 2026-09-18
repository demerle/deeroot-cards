using UnityEngine;
using UnityEngine.UI;

public class SimulatedSelection : MonoBehaviour
{
	private HoverEvent hoverEvent;

	private Button button;

	private void Start()
	{
		hoverEvent = GetComponent<HoverEvent>();
		button = GetComponent<Button>();
	}

	private void OnDisable()
	{
		Deselect();
	}

	public void Select()
	{
		hoverEvent.OnPointerEnter(null);
		button.targetGraphic.color = button.colors.highlightedColor;
	}

	public void Deselect()
	{
		hoverEvent.OnPointerExit(null);
		button.OnDeselect(null);
		button.targetGraphic.color = button.colors.normalColor;
	}
}
