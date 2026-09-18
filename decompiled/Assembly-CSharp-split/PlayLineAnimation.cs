using UnityEngine;

public class PlayLineAnimation : MonoBehaviour
{
	private LineEffect lineEffect;

	public AnimationCurve offsetCurve;

	public float offsetSpeed = 1f;

	public AnimationCurve widthCurve;

	public float widthSpeed = 1f;

	private void Start()
	{
		lineEffect = GetComponent<LineEffect>();
	}

	public void PlayOffset()
	{
		lineEffect.PlayAnim(LineEffect.AnimType.Offset, offsetCurve, offsetSpeed);
	}

	public void PlayWidth()
	{
		lineEffect.PlayAnim(LineEffect.AnimType.Width, widthCurve, widthSpeed);
	}
}
