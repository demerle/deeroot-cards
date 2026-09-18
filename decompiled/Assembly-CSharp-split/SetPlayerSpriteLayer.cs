using UnityEngine;

public class SetPlayerSpriteLayer : MonoBehaviour
{
	private SpriteMask[] sprites;

	private bool simpleSkin;

	private void Start()
	{
		simpleSkin = GetComponent<PlayerSkinHandler>().simpleSkin;
		Player componentInParent = GetComponentInParent<Player>();
		int num = SortingLayer.NameToID("Player" + (componentInParent.playerID + 1));
		setSpriteLayerOfChildren(GetComponentInParent<Holding>().holdable.gameObject, num);
		setSpriteLayerOfChildren(base.gameObject, num);
		if (!simpleSkin)
		{
			GetComponent<PlayerSkinHandler>().InitSpriteMask(num);
		}
	}

	private void setSpriteLayerOfChildren(GameObject obj, int layer)
	{
		sprites = obj.transform.root.GetComponentsInChildren<SpriteMask>();
		for (int i = 0; i < sprites.Length; i++)
		{
			if (simpleSkin)
			{
				sprites[i].enabled = false;
				sprites[i].GetComponent<SpriteRenderer>().enabled = true;
			}
			else
			{
				sprites[i].frontSortingLayerID = layer;
				sprites[i].backSortingLayerID = layer;
			}
		}
	}

	public void ToggleSimple(bool isSimple)
	{
		simpleSkin = isSimple;
	}
}
