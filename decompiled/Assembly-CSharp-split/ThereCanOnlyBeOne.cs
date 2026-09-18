using UnityEngine;
using UnityEngine.Events;

public class ThereCanOnlyBeOne : MonoBehaviour
{
	public UnityEvent PokeEvent;

	private void Start()
	{
		bool flag = false;
		ThereCanOnlyBeOne[] componentsInChildren = base.transform.root.GetComponentsInChildren<ThereCanOnlyBeOne>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (!(componentsInChildren[i] == this))
			{
				flag = true;
				componentsInChildren[i].Poke();
			}
		}
		if (flag)
		{
			Object.Destroy(base.gameObject);
		}
	}

	public void Poke()
	{
		PokeEvent.Invoke();
	}
}
