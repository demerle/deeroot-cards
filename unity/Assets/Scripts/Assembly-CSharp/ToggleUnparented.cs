using UnityEngine;

public class ToggleUnparented : MonoBehaviour
{
	private Unparent[] unparents;

	private void Awake()
	{
		unparents = GetComponentsInChildren<Unparent>();
	}

	private void OnDisable()
	{
		for (int i = 0; i < unparents.Length; i++)
		{
			unparents[i].gameObject.SetActive(false);
		}
		GetComponent<Holding>().holdable.gameObject.SetActive(false);
	}

	private void OnEnable()
	{
		for (int i = 0; i < unparents.Length; i++)
		{
			unparents[i].gameObject.SetActive(true);
		}
		GetComponent<Holding>().holdable.gameObject.SetActive(false);
	}
}
