using Photon.Pun;
using UnityEngine;

public class CharacterCreatorItemEquipper : MonoBehaviour
{
	public GameObject itemParent;

	public GameObject defaultEyes;

	public GameObject defaultMouth;

	private CharacterCreatorItemLoader itemLoader;

	private CharacterCreator creator;

	public float scaleM = 1f;

	private bool spawnedSpecific;

	private bool inited;

	private void Start()
	{
		Init();
		if ((bool)creator && !spawnedSpecific)
		{
			Equip(defaultEyes.GetComponent<CharacterItem>(), CharacterItemType.Eyes);
			Equip(defaultMouth.GetComponent<CharacterItem>(), CharacterItemType.Mouth);
		}
	}

	public void SpawnPlayerFace(PlayerFace newFace)
	{
		spawnedSpecific = true;
		EquipFace(newFace);
	}

	private void Init()
	{
		if (!inited)
		{
			inited = true;
			creator = GetComponent<CharacterCreator>();
			itemLoader = CharacterCreatorItemLoader.instance;
		}
	}

	[PunRPC]
	public void RPCA_SetFace(int eyeID, Vector2 eyeOffset, int mouthID, Vector2 mouthOffset, int detailID, Vector2 detailOffset, int detail2ID, Vector2 detail2Offset)
	{
		PlayerFace face = PlayerFace.CreateFace(eyeID, eyeOffset, mouthID, mouthOffset, detailID, detailOffset, detail2ID, detail2Offset);
		EquipFace(face);
	}

	public void EquipFace(PlayerFace face)
	{
		Init();
		Equip(itemLoader.GetItem(face.eyeID, CharacterItemType.Eyes), CharacterItemType.Eyes, face.eyeOffset);
		Equip(itemLoader.GetItem(face.mouthID, CharacterItemType.Mouth), CharacterItemType.Mouth, face.mouthOffset);
		Equip(itemLoader.GetItem(face.detailID, CharacterItemType.Detail), CharacterItemType.Detail, face.detailOffset);
		Equip(itemLoader.GetItem(face.detail2ID, CharacterItemType.Detail), CharacterItemType.Detail, face.detail2Offset, 1);
	}

	public void Equip(CharacterItem newSprite, CharacterItemType itemType, Vector2 offset = default(Vector2), int slotNr = 0)
	{
		if (newSprite == null)
		{
			return;
		}
		Init();
		if ((bool)creator)
		{
			if (itemType == CharacterItemType.Eyes)
			{
				creator.currentPlayerFace.eyeID = itemLoader.GetItemID(newSprite, itemType);
			}
			if (itemType == CharacterItemType.Mouth)
			{
				creator.currentPlayerFace.mouthID = itemLoader.GetItemID(newSprite, itemType);
			}
			if (itemType == CharacterItemType.Detail)
			{
				if (slotNr == 0)
				{
					creator.currentPlayerFace.detailID = itemLoader.GetItemID(newSprite, itemType);
				}
				if (slotNr == 1)
				{
					creator.currentPlayerFace.detail2ID = itemLoader.GetItemID(newSprite, itemType);
				}
			}
		}
		Clear(itemType, slotNr);
		SpawnItem(newSprite, itemType, offset, slotNr);
		CopyChildren[] componentsInChildren = GetComponentsInChildren<CopyChildren>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].DoUpdate();
		}
	}

	private void SpawnItem(CharacterItem newSprite, CharacterItemType itemType, Vector2 offset = default(Vector2), int slotNr = 0)
	{
		GameObject obj = Object.Instantiate(newSprite.gameObject);
		obj.gameObject.SetActive(value: true);
		obj.transform.SetParent(itemParent.transform);
		obj.GetComponent<SpriteRenderer>().sprite = newSprite.GetComponent<CharacterItem>().sprite;
		obj.GetComponent<SpriteRenderer>().sortingOrder = newSprite.GetComponent<CharacterItem>().GetComponent<SpriteRenderer>().sortingOrder;
		obj.GetComponent<CharacterItem>().itemType = itemType;
		obj.GetComponent<SpriteRenderer>().color = newSprite.GetComponent<SpriteRenderer>().color;
		obj.GetComponentInChildren<CharacterItem>().offset = newSprite.GetComponent<CharacterItem>().offset;
		obj.GetComponentInChildren<CharacterItem>().sprite = newSprite.GetComponent<CharacterItem>().sprite;
		obj.GetComponentInChildren<CharacterItem>().slotNr = slotNr;
		obj.transform.localScale = newSprite.GetComponent<CharacterItem>().scale * Vector3.one * scaleM;
		obj.transform.localPosition = (newSprite.GetComponent<CharacterItem>().offset + offset) * scaleM;
		creator?.SetOffset(offset, itemType, slotNr);
	}

	private void Clear(CharacterItemType itemType, int slotNr = 0)
	{
		for (int i = 0; i < itemParent.transform.childCount; i++)
		{
			CharacterItem component = itemParent.transform.GetChild(i).GetComponent<CharacterItem>();
			if (component.itemType == itemType && component.slotNr == slotNr)
			{
				Object.DestroyImmediate(itemParent.transform.GetChild(i).gameObject);
			}
		}
	}
}
