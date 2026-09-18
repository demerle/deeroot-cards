using SoundImplementation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class HoverEvent : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	public UnityEvent enterEvent;

	public UnityEvent exitEvent;

	public bool isHovered;

	public bool isSelected;

	public void OnPointerEnter(PointerEventData eventData)
	{
		SoundPlayerStatic.Instance.PlayButtonHover();
		enterEvent.Invoke();
		isHovered = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		exitEvent.Invoke();
		isHovered = false;
	}

	private void Update()
	{
		isSelected = EventSystem.current.currentSelectedGameObject == base.gameObject;
	}

	private void OnDisable()
	{
		isHovered = false;
	}
}
