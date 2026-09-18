using UnityEngine;
using UnityEngine.UI.ProceduralImage;

[ModifierID("Uniform")]
public class UniformModifier : ProceduralImageModifier
{
	[SerializeField]
	private float radius;

	public float Radius
	{
		get
		{
			return radius;
		}
		set
		{
			radius = value;
			base._Graphic.SetVerticesDirty();
		}
	}

	public override Vector4 CalculateRadius(Rect imageRect)
	{
		float num = radius;
		return new Vector4(num, num, num, num);
	}
}
