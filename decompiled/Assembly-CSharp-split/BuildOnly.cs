using UnityEngine;

public class BuildOnly : MonoBehaviour
{
	private void Start()
	{
		if (Application.isEditor)
		{
			base.gameObject.SetActive(value: false);
		}
	}
}
