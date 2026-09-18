namespace UnityEngine.UI.ProceduralImage
{
	[DisallowMultipleComponent]
	public abstract class ProceduralImageModifier : MonoBehaviour
	{
		protected Graphic graphic;

		protected Graphic _Graphic
		{
			get
			{
				if (graphic == null)
				{
					graphic = GetComponent<Graphic>();
				}
				return graphic;
			}
		}

		public abstract Vector4 CalculateRadius(Rect imageRect);
	}
}
