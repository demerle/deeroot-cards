using System.Collections;
using Photon.Pun;
using UnityEngine;

public class ScaleShake : MonoBehaviour
{
	public bool useTimeScale = true;

	public float targetScale = 1f;

	public float multiplier = 1f;

	private float velocity;

	public float drag = 1f;

	public float spring = 1f;

	public float clampVelocity;

	internal float high;

	internal float low;

	private void Update()
	{
		float num = Mathf.Clamp(useTimeScale ? TimeHandler.deltaTime : Time.unscaledDeltaTime, 0f, 0.02f);
		if (clampVelocity != 0f)
		{
			velocity = Mathf.Clamp(velocity, 0f - clampVelocity, clampVelocity);
		}
		velocity += (targetScale - base.transform.localScale.x) * num * 50f * spring;
		velocity -= drag * velocity * 20f * num;
		base.transform.localScale += Vector3.one * (velocity * 10f * num);
	}

	public void AddForce(float force)
	{
		velocity += force * multiplier * 5f;
	}

	public void AddForce()
	{
		velocity += 1f * multiplier * 5f;
	}

	public void SetTarget(float target)
	{
		targetScale = target;
	}

	public void SetHigh()
	{
		targetScale = high;
	}

	public void SetLow()
	{
		targetScale = low;
	}

	public void ScaleOutRootPhoton()
	{
		StartCoroutine(GoAwayOutRootPhoton());
	}

	private IEnumerator GoAwayOutRootPhoton()
	{
		while (base.transform.localScale.x > 0f)
		{
			targetScale = 0f;
			yield return null;
		}
		PhotonNetwork.Destroy(base.transform.root.gameObject);
	}
}
