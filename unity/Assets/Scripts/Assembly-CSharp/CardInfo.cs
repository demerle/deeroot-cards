using Photon.Pun;
using Sirenix.OdinInspector;
using UnityEngine;

public class CardInfo : MonoBehaviour
{
	public enum Rarity
	{
		Common = 0,
		Uncommon = 1,
		Rare = 2
	}

	[Header("Sound Settings")]
	public bool soundDisableBlockBasic;

	[Header("Settings")]
	public string cardName = "";

	[TextArea]
	public string cardDestription = "";

	public CardInfoStat[] cardStats;

	public Rarity rarity;

	public GameObject cardArt;

	public Sprite sprite;

	[HideInInspector]
	public CardInfo sourceCard;

	public Color cardColor = new Color(0.14509805f, 0.14509805f, 0.14509805f);

	public CardCategory[] categories;

	[FoldoutGroup("Restrictions", 0)]
	public bool allowMultiple = true;

	[FoldoutGroup("Restrictions", 0)]
	public CardCategory[] blacklistedCategories;

	public GameObject cardBase;

	public CardThemeColor.CardThemeColorType colorTheme;

	private void Awake()
	{
		sourceCard = CardChoice.instance.GetSourceCard(this);
		cardBase = Object.Instantiate(cardBase, base.transform.position, base.transform.rotation);
		cardBase.transform.SetParent(base.transform, true);
		bool charge = false;
		Gun component = GetComponent<Gun>();
		if ((bool)component && component.useCharge)
		{
			charge = true;
		}
		cardBase.GetComponent<CardInfoDisplayer>().DrawCard(cardStats, cardName, cardDestription, sprite, charge);
		cardBase.GetComponentInChildren<GeneralParticleSystem>().particleSettings.randomColor = cardColor;
	}

	[PunRPC]
	public void RPCA_ChangeSelected(bool setSelected)
	{
		GetComponentInChildren<CardVisuals>().ChangeSelected(setSelected);
	}
}
