using System.Collections;
using UnityEngine;

public class GridBounceCircle : GridObject
{
	private float m_startSize;

	private bool isBlooping;

	private float currentDistance;

	private void Start()
	{
		m_startSize = base.transform.localScale.x;
	}

	public override void BopCall(float power)
	{
		if (!isBlooping || (isBlooping && power > currentDistance))
		{
			StopAllCoroutines();
			StartCoroutine(Blop(power));
		}
	}

	public override void OnSetSize(float size)
	{
		if (!isBlooping)
		{
			base.transform.localScale = Vector3.one * size * m_startSize;
		}
	}

	private IEnumerator Blop(float power)
	{
		isBlooping = true;
		currentDistance = power;
		float maxSize = Mathf.Lerp(1f * m_startSize, 3f * m_startSize, power);
		float timer2 = 0f;
		while (timer2 < 1f)
		{
			timer2 += TimeHandler.deltaTime * 10f;
			base.transform.localScale = Vector3.one * Mathf.Lerp(m_startSize, maxSize, timer2);
			yield return null;
		}
		yield return new WaitForSeconds(0.2f);
		timer2 = 0f;
		while (timer2 < 1f)
		{
			timer2 += TimeHandler.deltaTime * 6f;
			base.transform.localScale = Vector3.one * Mathf.Lerp(maxSize, m_startSize, timer2);
			yield return null;
		}
		base.transform.localScale = Vector3.one * m_startSize;
		isBlooping = false;
	}
}
