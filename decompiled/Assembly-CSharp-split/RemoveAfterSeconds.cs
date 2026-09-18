using UnityEngine;

public class RemoveAfterSeconds : MonoBehaviour
{
	public float seconds = 3f;

	private float startSeconds;

	public bool shrink;

	public bool scaleWithScale;

	private float shrinkSpeed = 1f;

	private Vector3 startScale;

	private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	private float counter;

	private void Start()
	{
		startSeconds = seconds;
		startScale = base.transform.localScale;
		shrinkSpeed = Random.Range(1.5f, 2.5f);
	}

	private void Update()
	{
		seconds -= TimeHandler.deltaTime * (scaleWithScale ? (1f / base.transform.localScale.x) : 1f);
		if (!(seconds < 0f))
		{
			return;
		}
		if (shrink)
		{
			counter += Time.deltaTime * shrinkSpeed;
			base.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, curve.Evaluate(counter));
			if (counter > 1f)
			{
				Object.Destroy(base.gameObject);
			}
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}

	public void ResetCounter()
	{
		seconds = startSeconds;
	}
}
