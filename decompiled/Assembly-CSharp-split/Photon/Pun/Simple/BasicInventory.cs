using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.Simple;

public class BasicInventory : Inventory<Vector3Int>
{
	[SerializeField]
	public Vector3Int capacity = new Vector3Int(16, 1, 1);

	public int Volume => capacity.x * capacity.y * capacity.z;

	public int Used
	{
		get
		{
			int num = 0;
			List<IMountable> mountedObjs = base.DefaultMount.mountedObjs;
			int i = 0;
			for (int count = mountedObjs.Count; i < count; i++)
			{
				if (mountedObjs[i] is IInventoryable<Vector3Int> { Size: var size })
				{
					int num2 = size.x * size.y * size.z;
					num += num2;
				}
			}
			return num;
		}
	}

	public int Remaining => Volume - Used;

	public override bool TestCapacity(IInventoryable<Vector3Int> inventoryable)
	{
		Vector3Int size = inventoryable.Size;
		return size.x * size.y * size.z <= Remaining;
	}
}
