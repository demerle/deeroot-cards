using UnityEngine;

public class AddShake : MonoBehaviour
{
	public Vector2 shake;

	public bool auto = true;

	public bool inheritScale;

	public float max = float.PositiveInfinity;

	private void Start()
	{
		if (auto)
		{
			DoShake();
		}
	}

	public void DoShake()
	{
		GamefeelManager.GameFeel(shake * Mathf.Clamp(inheritScale ? base.transform.localScale.x : 1f, 0f, max));
	}
}
