using UnityEngine;
using UnityEngine.Events;

public class DuringReloadTrigger : MonoBehaviour
{
	public UnityEvent triggerEvent;

	public UnityEvent triggerStartEvent;

	public UnityEvent triggerEndEvent;

	private Gun gunAmmo;

	private bool triggering;

	private void Start()
	{
		gunAmmo = GetComponentInParent<WeaponHandler>().gun;
	}

	private void Update()
	{
		if (gunAmmo.isReloading)
		{
			if (!triggering)
			{
				triggerStartEvent.Invoke();
				triggering = true;
			}
			triggerEvent.Invoke();
		}
		else if (triggering)
		{
			triggerEndEvent.Invoke();
			triggering = false;
		}
	}
}
