using InControl;
using UnityEngine;
using UnityEngine.Events;

public class GoBack : MonoBehaviour
{
	public UnityEvent goBackEvent;

	public ListMenuPage target;

	private void Start()
	{
	}

	private void Update()
	{
		for (int i = 0; i < InputManager.ActiveDevices.Count; i++)
		{
			if (InputManager.ActiveDevices[i].Action2.WasPressed)
			{
				goBackEvent.Invoke();
				target.Open();
			}
		}
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			goBackEvent.Invoke();
			target.Open();
		}
	}
}
