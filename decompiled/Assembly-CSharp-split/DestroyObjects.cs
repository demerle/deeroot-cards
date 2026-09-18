using UnityEngine;

public class DestroyObjects : MonoBehaviour
{
	public GameObject[] objectsToDestroy;

	public void DestroySelf()
	{
		Object.Destroy(base.gameObject);
	}

	public void DestroyAllObjects()
	{
		for (int i = 0; i < objectsToDestroy.Length; i++)
		{
			Object.Destroy(objectsToDestroy[i]);
		}
	}
}
