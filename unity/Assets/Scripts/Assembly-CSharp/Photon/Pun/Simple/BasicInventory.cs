using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.Simple
{
	public class BasicInventory : Inventory<Vector3Int>
	{
		[SerializeField]
		public Vector3Int capacity = new Vector3Int(16, 1, 1);

		public int Volume
		{
			get
			{
				return capacity.x * capacity.y * capacity.z;
			}
		}

		public int Used
		{
			get
			{
				int num = 0;
				List<IMountable> mountedObjs = base.DefaultMount.mountedObjs;
				int i = 0;
				for (int count = mountedObjs.Count; i < count; i++)
				{
					IInventoryable<Vector3Int> inventoryable = mountedObjs[i] as IInventoryable<Vector3Int>;
					if (inventoryable != null)
					{
						Vector3Int size = inventoryable.Size;
						int num2 = size.x * size.y * size.z;
						num += num2;
					}
				}
				return num;
			}
		}

		public int Remaining
		{
			get
			{
				return Volume - Used;
			}
		}

		public override bool TestCapacity(IInventoryable<Vector3Int> inventoryable)
		{
			Vector3Int size = inventoryable.Size;
			return size.x * size.y * size.z <= Remaining;
		}
	}
}
