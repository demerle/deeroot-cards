using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ColorHandler : MonoBehaviour
{
	public enum ColorType
	{
		PhysicsObject = 0
	}

	public Dictionary<ColorType, Color> colors = new Dictionary<ColorType, Color>();

	public static ColorHandler instance;

	private void Awake()
	{
		instance = this;
	}

	public Color GetColor(ColorType colorType)
	{
		return colors[colorType];
	}
}
