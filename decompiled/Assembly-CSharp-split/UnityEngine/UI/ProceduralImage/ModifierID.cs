using System;

namespace UnityEngine.UI.ProceduralImage;

[AttributeUsage(AttributeTargets.Class)]
public class ModifierID : Attribute
{
	private string name;

	public string Name => name;

	public ModifierID(string name)
	{
		this.name = name;
	}
}
