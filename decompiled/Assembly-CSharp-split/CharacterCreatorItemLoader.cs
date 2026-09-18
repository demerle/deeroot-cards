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
			return itemType switch
			{
				CharacterItemType.Eyes => eyes[itemID], 
				CharacterItemType.Mouth => mouths[itemID], 
				_ => accessories[itemID], 
			};
		}
		catch
		{
			return null;
		}
	}

	internal int GetItemID(CharacterItem newSprite, CharacterItemType itemType)
	{
		CharacterItem[] array = null;
		array = itemType switch
		{
			CharacterItemType.Eyes => eyes, 
			CharacterItemType.Mouth => mouths, 
			_ => accessories, 
		};
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
