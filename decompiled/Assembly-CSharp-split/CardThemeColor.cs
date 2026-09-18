using System;
using UnityEngine;

[Serializable]
public class CardThemeColor
{
	public enum CardThemeColorType
	{
		DestructiveRed,
		FirepowerYellow,
		DefensiveBlue,
		TechWhite,
		EvilPurple,
		PoisonGreen,
		NatureBrown,
		ColdBlue,
		MagicPink
	}

	public CardThemeColorType themeType;

	public Color targetColor;

	public Color bgColor;
}
