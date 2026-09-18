using UnityEngine;

public class CharacterCreatorButtonSpawner : MonoBehaviour
{
	public GameObject sourceButton;

	private CharacterCreatorItemLoader loader;

	private CharacterCreator creator;

	private void Start()
	{
		creator = GetComponent<CharacterCreator>();
		loader = CharacterCreatorItemLoader.instance;
		OpenMenu(CharacterItemType.Eyes, 0);
	}

	public void OpenMenu(int id)
	{
		if (id == 0)
		{
			OpenMenu(CharacterItemType.Eyes, 0);
		}
		if (id == 1)
		{
			OpenMenu(CharacterItemType.Mouth, 0);
		}
		if (id == 2)
		{
			OpenMenu(CharacterItemType.Detail, 0);
		}
		if (id == 3)
		{
			OpenMenu(CharacterItemType.Detail, 1);
		}
	}

	public void OpenMenu(CharacterItemType target, int slotNr)
	{
		CharacterItem[] array = null;
		if (target == CharacterItemType.Eyes)
		{
			array = loader.eyes;
		}
		if (target == CharacterItemType.Mouth)
		{
			array = loader.mouths;
		}
		if (target == CharacterItemType.Detail)
		{
			array = loader.accessories;
		}
		for (int i = 0; i < sourceButton.transform.parent.childCount; i++)
		{
			if (sourceButton.transform.parent.GetChild(i).gameObject.activeSelf)
			{
				Object.Destroy(sourceButton.transform.parent.GetChild(i).gameObject);
			}
		}
		for (int j = 0; j < array.Length; j++)
		{
			GameObject gameObject = Object.Instantiate(sourceButton, sourceButton.transform.parent);
			gameObject.SetActive(value: true);
			Transform parent = gameObject.transform.Find("ItemParent");
			GameObject gameObject2 = Object.Instantiate(array[j].gameObject, parent);
			gameObject.GetComponent<CharacterItemButton>().itemType = target;
			gameObject.GetComponent<CharacterItemButton>().slotNr = slotNr;
			gameObject.GetComponentInChildren<CharacterItem>().sprite = array[j].gameObject.GetComponent<SpriteRenderer>().sprite;
			gameObject2.GetComponentInChildren<CharacterItem>().GetComponent<SpriteRenderer>().sortingOrder = array[j].gameObject.GetComponent<SpriteRenderer>().sortingOrder;
			gameObject2.GetComponentInChildren<CharacterItem>().scale = array[j].scale;
			gameObject2.GetComponentInChildren<CharacterItem>().itemType = target;
			gameObject2.GetComponentInChildren<CharacterItem>().offset = array[j].offset;
			gameObject2.GetComponentInChildren<CharacterItem>().slotNr = slotNr;
			gameObject2.GetComponentInChildren<SpriteRenderer>().transform.localPosition = array[j].offset;
			gameObject2.GetComponentInChildren<SpriteRenderer>().transform.localScale = array[j].scale * Vector2.one;
			if (target == CharacterItemType.Eyes && j == creator.currentPlayerFace.eyeID)
			{
				gameObject.transform.Find("SelectedDot").gameObject.SetActive(value: true);
			}
			if (target == CharacterItemType.Mouth && j == creator.currentPlayerFace.mouthID)
			{
				gameObject.transform.Find("SelectedDot").gameObject.SetActive(value: true);
			}
			if (target == CharacterItemType.Detail && j == creator.currentPlayerFace.detailID && slotNr == 0)
			{
				gameObject.transform.Find("SelectedDot").gameObject.SetActive(value: true);
			}
			if (target == CharacterItemType.Detail && slotNr == 1 && j == creator.currentPlayerFace.detail2ID)
			{
				gameObject.transform.Find("SelectedDot").gameObject.SetActive(value: true);
			}
		}
	}

	public void SelectButton(CharacterItemType itemType, int slotNr)
	{
		for (int i = 0; i < sourceButton.transform.parent.childCount; i++)
		{
			GameObject gameObject = sourceButton.transform.parent.GetChild(i).gameObject;
			if (itemType == CharacterItemType.Eyes)
			{
				if (i - 1 == creator.currentPlayerFace.eyeID)
				{
					gameObject.transform.Find("SelectedDot").gameObject.SetActive(value: true);
				}
				else
				{
					gameObject.transform.Find("SelectedDot").gameObject.SetActive(value: false);
				}
			}
			if (itemType == CharacterItemType.Mouth)
			{
				if (i - 1 == creator.currentPlayerFace.mouthID)
				{
					gameObject.transform.Find("SelectedDot").gameObject.SetActive(value: true);
				}
				else
				{
					gameObject.transform.Find("SelectedDot").gameObject.SetActive(value: false);
				}
			}
			if (itemType == CharacterItemType.Detail)
			{
				if (i - 1 == creator.currentPlayerFace.detailID && slotNr == 0)
				{
					gameObject.transform.Find("SelectedDot").gameObject.SetActive(value: true);
				}
				else
				{
					gameObject.transform.Find("SelectedDot").gameObject.SetActive(value: false);
				}
			}
			if (itemType == CharacterItemType.Detail && slotNr == 1)
			{
				if (i - 1 == creator.currentPlayerFace.detail2ID)
				{
					gameObject.transform.Find("SelectedDot").gameObject.SetActive(value: true);
				}
				else
				{
					gameObject.transform.Find("SelectedDot").gameObject.SetActive(value: false);
				}
			}
		}
	}
}
