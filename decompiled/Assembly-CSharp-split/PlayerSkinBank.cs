using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Skin Bank", menuName = "Custom/Skin Bank", order = 99999)]
public class PlayerSkinBank : ScriptableObject
{
	[Serializable]
	public struct PlayerSkinInstance
	{
		public PlayerSkin currentPlayerSkin;
	}

	private static PlayerSkinBank instance;

	public PlayerSkinInstance[] skins = new PlayerSkinInstance[0];

	private static PlayerSkinBank Instance
	{
		get
		{
			if (instance == null)
			{
				instance = Resources.Load("SkinBank") as PlayerSkinBank;
			}
			return instance;
		}
	}

	public static PlayerSkin GetPlayerSkinColors(int team)
	{
		return Instance.skins[team].currentPlayerSkin;
	}

	public static PlayerSkinInstance GetPlayerSkin(int team)
	{
		return Instance.skins[team];
	}
}
