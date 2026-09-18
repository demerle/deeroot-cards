using Sirenix.OdinInspector;
using UnityEngine;

public class CharacterItem : MonoBehaviour
{
	public Sprite sprite;

	public float scale = 1f;

	public Vector2 offset = Vector2.zero;

	public CharacterItemType itemType;

	internal int slotNr;

	[ShowIf("itemType", CharacterItemType.Detail, true)]
	public float moveHealthBarUp;

	[Button]
	public void SaveTransform()
	{
		offset = base.transform.localPosition;
		scale = base.transform.localScale.x;
	}

	private void Start()
	{
		if (!base.transform.root.GetComponent<Player>())
		{
			return;
		}
		base.gameObject.AddComponent<CharacterItemMirror>();
		if (moveHealthBarUp != 0f)
		{
			HealthBar componentInChildren = base.transform.root.GetComponentInChildren<HealthBar>();
			if ((bool)componentInChildren)
			{
				componentInChildren.transform.localPosition += Vector3.up * moveHealthBarUp;
			}
		}
	}
}
