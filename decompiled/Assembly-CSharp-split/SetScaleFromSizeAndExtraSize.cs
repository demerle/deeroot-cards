using UnityEngine;

public class SetScaleFromSizeAndExtraSize : MonoBehaviour
{
	public float scalePerSize;

	private void Start()
	{
		float size = GetComponentInParent<RayCastTrail>().size;
		base.transform.localScale *= size * scalePerSize;
	}
}
