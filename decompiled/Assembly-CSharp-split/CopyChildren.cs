using UnityEngine;

public class CopyChildren : MonoBehaviour
{
	public GameObject target;

	public void DoUpdate()
	{
		for (int num = base.transform.childCount - 1; num >= 0; num--)
		{
			Object.Destroy(base.transform.GetChild(num).gameObject);
		}
		for (int i = 0; i < target.transform.childCount; i++)
		{
			Transform child = target.transform.GetChild(i);
			Object.Instantiate(child.gameObject, base.transform.TransformPoint(child.localPosition), Quaternion.identity, base.transform).transform.localScale = child.localScale;
		}
	}

	private void Update()
	{
		DoUpdate();
	}
}
