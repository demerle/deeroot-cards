using UnityEngine;
using UnityEngine.EventSystems;

public class CardBarButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	internal CardInfo card;

	public void OnPointerEnter(PointerEventData eventData)
	{
		GetComponentInParent<CardBar>().OnHover(card, base.transform.position);
		base.transform.localScale = Vector3.one * 1.1f;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		GetComponentInParent<CardBar>().StopHover();
		base.transform.localScale = Vector3.one * 1f;
	}
}
