using UnityEngine;

namespace Photon.Pun.Simple;

public abstract class ContactReactorBase : NetComponent, IContactReactor
{
	[HideInInspector]
	public ContactType triggerOn = (ContactType)15;

	protected SyncState syncState;

	protected int syncStateMountMask;

	public ContactType TriggerOn => triggerOn;

	public abstract bool IsPickup { get; }

	public override void OnAwakeInitialize(bool isNetObject)
	{
		if (isNetObject)
		{
			syncState = base.transform.GetNestedComponentInParent<SyncState, NetObject>();
			syncStateMountMask = (syncState ? syncState.mountableTo.mask : 0);
		}
		base.OnAwakeInitialize(isNetObject);
	}

	public virtual Consumption OnContactEvent(ContactEvent contactEvent)
	{
		ContactType contactType = contactEvent.contactType;
		if (triggerOn != ContactType.Undefined && (contactType & triggerOn) == 0)
		{
			return Consumption.None;
		}
		if (IsPickup)
		{
			int validMountsMask = contactEvent.contactSystem.ValidMountsMask;
			int num = syncState.mountableTo;
			if (validMountsMask != 0 && (validMountsMask & num) == 0)
			{
				return Consumption.None;
			}
		}
		return ProcessContactEvent(contactEvent);
	}

	protected abstract Consumption ProcessContactEvent(ContactEvent contactEvent);
}
public abstract class ContactReactorBase<T> : ContactReactorBase, IOnContactEvent where T : class, IContactSystem
{
}
