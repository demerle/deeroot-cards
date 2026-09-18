using System.Collections;
using UnityEngine;

public class Unparent : MonoBehaviour
{
	public Transform parent;

	public bool follow;

	public float destroyDelay;

	private bool done;

	private void Start()
	{
		parent = base.transform.root;
	}

	private void LateUpdate()
	{
		if (!done)
		{
			if (base.transform.root != null)
			{
				base.transform.SetParent(null, true);
			}
			if (follow && (bool)parent)
			{
				base.transform.position = parent.transform.position;
			}
			if (!parent)
			{
				StartCoroutine(DelayRemove());
				done = true;
			}
		}
	}

	private IEnumerator DelayRemove()
	{
		yield return new WaitForSeconds(destroyDelay);
		Object.Destroy(base.gameObject);
	}
}
