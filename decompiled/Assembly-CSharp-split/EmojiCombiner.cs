using UnityEngine;

public class EmojiCombiner : MonoBehaviour
{
	public GameObject currentEmoji;

	public void AddEmoji(GameObject newEmoji)
	{
		CharacterItem characterItem = FindEmojiPiece(currentEmoji, "M");
		CharacterItem characterItem2 = FindEmojiPiece(newEmoji, "M");
		float delta = GetDelta(characterItem, characterItem2);
		CharacterItem characterItem3 = FindEmojiPiece(currentEmoji, "E");
		CharacterItem characterItem4 = FindEmojiPiece(newEmoji, "E");
		float delta2 = GetDelta(characterItem3, characterItem4);
		if (delta > delta2)
		{
			Object.Destroy(characterItem.gameObject);
			GameObject obj = Object.Instantiate(characterItem2.gameObject);
			obj.transform.SetParent(currentEmoji.transform);
			obj.transform.localPosition = characterItem2.transform.localPosition;
		}
		else
		{
			Object.Destroy(characterItem3.gameObject);
			GameObject obj2 = Object.Instantiate(characterItem4.gameObject);
			obj2.transform.SetParent(currentEmoji.transform);
			obj2.transform.localPosition = characterItem4.transform.localPosition;
		}
	}

	private float GetDelta(CharacterItem from, CharacterItem to)
	{
		return 1f;
	}

	private CharacterItem FindEmojiPiece(GameObject emoji, string target)
	{
		target = target.ToUpper();
		CharacterItem[] componentsInChildren = emoji.GetComponentsInChildren<CharacterItem>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].name.ToUpper()[0] == target.ToCharArray()[0])
			{
				return componentsInChildren[i];
			}
		}
		return null;
	}
}
