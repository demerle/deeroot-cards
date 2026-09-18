using Photon.Pun.Simple.ContactGroups;
using UnityEngine;

namespace Photon.Pun.Simple;

public abstract class Inventory<T> : NetComponent, IInventorySystem<T>, IInventorySystem, IContactSystem
{
	[SerializeField]
	protected MountSelector defaultMounting = new MountSelector(0);

	[SerializeField]
	protected ContactGroupMaskSelector contactGroups;

	protected MountsManager mountsLookup;

	protected int defaultMountingMask;

	public Mount DefaultMount { get; set; }

	public IContactGroupMask ValidContactGroups => contactGroups;

	public byte SystemIndex { get; set; }

	public int ValidMountsMask => defaultMountingMask;

	public override void OnAwakeInitialize(bool isNetObject)
	{
		base.OnAwakeInitialize(isNetObject);
		base.transform.EnsureRootComponentExists<ContactManager, NetObject>();
		mountsLookup = netObj.transform.GetComponent<MountsManager>();
		defaultMountingMask = 1 << defaultMounting.id;
	}

	public override void OnStart()
	{
		base.OnStart();
		if ((bool)mountsLookup)
		{
			DefaultMount = mountsLookup.GetMount(defaultMounting);
		}
	}

	public virtual Consumption TryTrigger(IContactReactor reactor, ContactEvent contactEvent, int compatibleMounts)
	{
		if (!(reactor is IInventoryable<T>))
		{
			return Consumption.None;
		}
		if ((int)contactGroups != 0)
		{
			int num = contactEvent.contactTrigger.ContactGroupsAssign?.Mask ?? 0;
			if ((contactGroups.Mask & num) == 0)
			{
				return Consumption.None;
			}
		}
		if (!TestCapacity(reactor as IInventoryable<T>))
		{
			return Consumption.None;
		}
		if (compatibleMounts == defaultMountingMask || (compatibleMounts & defaultMountingMask) != 0)
		{
			return Consumption.All;
		}
		return Consumption.None;
	}

	public virtual Mount TryPickup(IContactReactor reactor, ContactEvent contactEvent)
	{
		return DefaultMount;
	}

	public virtual bool TestCapacity(IInventoryable<T> inventoryable)
	{
		return true;
	}
}
