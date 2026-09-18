using System;
using UnityEngine;

[Serializable]
public class CardThemeColor
{
	public enum CardThemeColorType
	{
		DestructiveRed = 0,
		FirepowerYellow = 1,
		DefensiveBlue = 2,
		TechWhite = 3,
		EvilPurple = 4,
		PoisonGreen = 5,
		NatureBrown = 6,
		ColdBlue = 7,
		MagicPink = 8
	}

	public CardThemeColorType themeType;

	public Color targetColor;

	public Color bgColor;
}
