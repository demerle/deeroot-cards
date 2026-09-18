using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class UnparentOnHit : MonoBehaviour
{
	public float destroyAfterSeconds = 2f;

	public UnityEvent unparentEvent;

	private bool done;

	private void Start()
	{
		ProjectileHit componentInParent = GetComponentInParent<ProjectileHit>();
		if ((bool)componentInParent)
		{
			componentInParent.AddHitAction(Unparent);
		}
	}

	public void Unparent()
	{
		if (!done)
		{
			done = true;
			base.transform.SetParent(null, true);
			StartCoroutine(DelayDestroy());
			unparentEvent.Invoke();
		}
	}

	private IEnumerator DelayDestroy()
	{
		yield return new WaitForSeconds(destroyAfterSeconds);
		Object.Destroy(base.gameObject);
	}
}
