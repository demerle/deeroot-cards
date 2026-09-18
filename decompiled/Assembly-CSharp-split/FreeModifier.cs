using UnityEngine;
using UnityEngine.UI.ProceduralImage;

[ModifierID("Free")]
public class FreeModifier : ProceduralImageModifier
{
	[SerializeField]
	private Vector4 radius;

	public Vector4 Radius
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
		return radius;
	}

	protected void OnValidate()
	{
		radius.x = Mathf.Max(0f, radius.x);
		radius.y = Mathf.Max(0f, radius.y);
		radius.z = Mathf.Max(0f, radius.z);
		radius.w = Mathf.Max(0f, radius.w);
	}
}
