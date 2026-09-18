using System;
using UnityEngine;
using UnityEngine.UI;

public class CardRarityColor : MonoBehaviour
{
	public Color uncommonColor;

	public Color rareColor;

	public Color uncommonColorOff;

	public Color rareColorOff;

	private void Awake()
	{
		CardVisuals componentInParent = GetComponentInParent<CardVisuals>();
		componentInParent.toggleSelectionAction = (Action<bool>)Delegate.Combine(componentInParent.toggleSelectionAction, new Action<bool>(Toggle));
	}

	public void Toggle(bool isOn)
	{
		CardInfo componentInParent = GetComponentInParent<CardInfo>();
		if (componentInParent.rarity == CardInfo.Rarity.Uncommon)
		{
			GetComponent<Image>().color = (isOn ? uncommonColor : uncommonColorOff);
		}
		if (componentInParent.rarity == CardInfo.Rarity.Rare)
		{
			GetComponent<Image>().color = (isOn ? rareColor : rareColorOff);
		}
	}
}
