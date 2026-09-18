using UnityEngine;

public class ApplyRootScale : MonoBehaviour
{
	public float amount = 0.5f;

	private void Start()
	{
		base.transform.localScale = Vector3.Lerp(base.transform.localScale, base.transform.root.localScale, amount);
	}
}
