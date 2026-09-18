using Sonigon;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardBar : MonoBehaviour
{
	[Header("Sounds")]
	public SoundEvent soundCardPick;

	[Header("Settings")]
	public GameObject pointer;

	[Header("Settings")]
	public GameObject cardPos;

	private GameObject source;

	private CardInfo ci;

	private GameObject currentCard;

	private void Start()
	{
		source = base.transform.GetChild(0).gameObject;
	}

	public void ClearBar()
	{
		if ((bool)currentCard)
		{
			Object.Destroy(currentCard);
		}
		for (int num = base.transform.childCount - 1; num >= 0; num--)
		{
			if (base.transform.GetChild(num).gameObject.activeSelf)
			{
				Object.Destroy(base.transform.GetChild(num).gameObject);
			}
		}
	}

	public void AddCard(CardInfo card)
	{
		SoundManager.Instance.Play(soundCardPick, base.transform);
		ci = card;
		GameObject obj = Object.Instantiate(source, source.transform.position, source.transform.rotation, source.transform.parent);
		obj.transform.localScale = Vector3.one;
		string cardName = card.cardName;
		cardName = cardName.Substring(0, 2);
		string text = cardName[0].ToString().ToUpper();
		if (cardName.Length > 1)
		{
			string text2 = cardName[1].ToString().ToLower();
			cardName = text + text2;
		}
		else
		{
			cardName = text;
		}
		obj.GetComponentInChildren<TextMeshProUGUI>().text = cardName;
		obj.GetComponent<CardBarButton>().card = card;
		obj.gameObject.SetActive(value: true);
	}

	public void OnHover(CardInfo card, Vector3 hoverPos)
	{
		if ((bool)currentCard)
		{
			Object.Destroy(currentCard);
		}
		currentCard = CardChoice.instance.AddCardVisual(card, cardPos.transform.position);
		Collider2D[] componentsInChildren = currentCard.transform.root.GetComponentsInChildren<Collider2D>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		currentCard.GetComponentInChildren<Canvas>().sortingLayerName = "MostFront";
		currentCard.GetComponentInChildren<GraphicRaycaster>().enabled = false;
		currentCard.GetComponentInChildren<SetScaleToZero>().enabled = false;
		currentCard.GetComponentInChildren<SetScaleToZero>().transform.localScale = Vector3.one * 1.15f;
	}

	public void StopHover()
	{
		if ((bool)currentCard)
		{
			Object.Destroy(currentCard);
		}
	}

	private void Update()
	{
	}
}
