using UnityEngine;

public class CharacterCreatorItemLoader : MonoBehaviour
{
	public CharacterItem[] eyes;

	public CharacterItem[] mouths;

	public CharacterItem[] accessories;

	public static CharacterCreatorItemLoader instance;

	private void Awake()
	{
		instance = this;
	}

	private void Update()
	{
	}

	internal CharacterItem GetItem(int itemID, CharacterItemType itemType)
	{
		try
		{
			switch (itemType)
			{
			case CharacterItemType.Eyes:
				return eyes[itemID];
			case CharacterItemType.Mouth:
				return mouths[itemID];
			default:
				return accessories[itemID];
			}
		}
		catch
		{
			return null;
		}
	}

	internal int GetItemID(CharacterItem newSprite, CharacterItemType itemType)
	{
		CharacterItem[] array = null;
		switch (itemType)
		{
		case CharacterItemType.Eyes:
			array = eyes;
			break;
		case CharacterItemType.Mouth:
			array = mouths;
			break;
		default:
			array = accessories;
			break;
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].sprite == newSprite.sprite)
			{
				return i;
			}
		}
		return -1;
	}

	public void UpdateItems(CharacterItemType target, CharacterItem[] items)
	{
		for (int i = 0; i < items.Length; i++)
		{
			items[i].sprite = items[i].GetComponent<SpriteRenderer>().sprite;
		}
		if (target == CharacterItemType.Eyes)
		{
			eyes = items;
		}
		if (target == CharacterItemType.Mouth)
		{
			mouths = items;
		}
		if (target == CharacterItemType.Detail)
		{
			accessories = items;
		}
	}
}
