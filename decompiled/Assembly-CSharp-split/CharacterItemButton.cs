using SoundImplementation;
using UnityEngine;

public class CharacterItemButton : MonoBehaviour
{
	public CharacterItemType itemType;

	public int slotNr;

	public void Click()
	{
		SoundPlayerStatic.Instance.PlayButtonClick();
		GetComponentInParent<CharacterCreatorItemEquipper>().Equip(base.gameObject.GetComponentInChildren<CharacterItem>(), itemType, Vector2.zero, slotNr);
		GetComponentInParent<CharacterCreatorButtonSpawner>().SelectButton(itemType, slotNr);
	}
}
