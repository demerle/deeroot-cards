using UnityEngine;

public class DisableObjects : MonoBehaviour
{
	public bool playOnAwake = true;

	public GameObject[] objects;

	private void Start()
	{
		if (playOnAwake)
		{
			DoIt();
		}
	}

	private void DoIt()
	{
		for (int i = 0; i < objects.Length; i++)
		{
			objects[i].SetActive(value: false);
		}
	}
}
