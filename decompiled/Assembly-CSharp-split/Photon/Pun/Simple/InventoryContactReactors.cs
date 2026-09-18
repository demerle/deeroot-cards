using UnityEngine;

namespace Photon.Pun.Simple;

public class InventoryContactReactors : InventoryContactReactors<Vector3Int>
{
	[SerializeField]
	protected Vector3Int size = new Vector3Int(1, 1, 1);

	public override Vector3Int Size => size;

	public override void OnAwakeInitialize(bool isNetObject)
	{
		base.OnAwakeInitialize(isNetObject);
		volume = size.x * size.y * size.z;
	}
}
public abstract class InventoryContactReactors<T> : ContactReactorBase<IInventorySystem<T>>, IInventoryable<T>, IContactable
{
	protected int volume;

	public abstract T Size { get; }

	public int Volume => volume;

	public override bool IsPickup => true;

	protected override Consumption ProcessContactEvent(ContactEvent contactEvent)
	{
		if (!(contactEvent.contactSystem is IInventorySystem<T> inventorySystem))
		{
			return Consumption.None;
		}
		if (IsPickup)
		{
			Mount mount = inventorySystem.TryPickup(this, contactEvent);
			if ((bool)mount)
			{
				syncState.HardMount(mount);
			}
		}
		return Consumption.All;
	}
}
