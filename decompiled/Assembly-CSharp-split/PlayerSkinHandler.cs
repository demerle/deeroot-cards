using UnityEngine;

public class PlayerSkinHandler : MonoBehaviour
{
	public bool simpleSkin;

	private PlayerSkinParticle[] skins;

	private CharacterData data;

	private bool inited;

	private void Start()
	{
		Init();
	}

	private void Init()
	{
		if (!inited)
		{
			inited = true;
			ToggleSimpleSkin(simpleSkin);
			data = GetComponentInParent<CharacterData>();
			if (!simpleSkin)
			{
				GameObject gameObject = Object.Instantiate(PlayerSkinBank.GetPlayerSkinColors(data.player.playerID).gameObject, base.transform.position, base.transform.rotation, base.transform);
				skins = gameObject.GetComponentsInChildren<PlayerSkinParticle>();
			}
		}
	}

	public void TakeDamageBlink(Vector2 damage, bool selfDamage)
	{
		BlinkColor(Color.white * 0.95f);
	}

	public void BlinkColor(Color blinkColor)
	{
		if (skins != null)
		{
			for (int i = 0; i < skins.Length; i++)
			{
				skins[i].BlinkColor(blinkColor);
			}
		}
	}

	public void InitSpriteMask(int spriteLayerID)
	{
		Init();
		for (int i = 0; i < skins.Length; i++)
		{
			skins[i].Init(spriteLayerID);
		}
	}

	public void ToggleSimpleSkin(bool isSimple)
	{
		simpleSkin = isSimple;
		GetComponent<SetPlayerSpriteLayer>().ToggleSimple(isSimple);
	}
}
