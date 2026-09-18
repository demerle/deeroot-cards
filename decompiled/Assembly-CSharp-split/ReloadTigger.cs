using System;
using UnityEngine;
using UnityEngine.Events;

public class ReloadTigger : MonoBehaviour
{
	public UnityEvent reloadDoneEvent;

	public UnityEvent outOfAmmoEvent;

	private void Start()
	{
		CharacterStatModifiers componentInParent = GetComponentInParent<CharacterStatModifiers>();
		componentInParent.OnReloadDoneAction = (Action<int>)Delegate.Combine(componentInParent.OnReloadDoneAction, new Action<int>(OnReloadDone));
		CharacterStatModifiers componentInParent2 = GetComponentInParent<CharacterStatModifiers>();
		componentInParent2.OutOfAmmpAction = (Action<int>)Delegate.Combine(componentInParent2.OutOfAmmpAction, new Action<int>(OnOutOfAmmo));
	}

	private void OnDestroy()
	{
		CharacterStatModifiers componentInParent = GetComponentInParent<CharacterStatModifiers>();
		componentInParent.OnReloadDoneAction = (Action<int>)Delegate.Remove(componentInParent.OnReloadDoneAction, new Action<int>(OnReloadDone));
		CharacterStatModifiers componentInParent2 = GetComponentInParent<CharacterStatModifiers>();
		componentInParent2.OutOfAmmpAction = (Action<int>)Delegate.Remove(componentInParent2.OutOfAmmpAction, new Action<int>(OnOutOfAmmo));
	}

	private void OnReloadDone(int bulletsReloaded)
	{
		reloadDoneEvent.Invoke();
	}

	private void OnOutOfAmmo(int bulletsReloaded)
	{
		outOfAmmoEvent.Invoke();
	}

	private void Update()
	{
	}
}
