using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.Simple
{
	[RequireComponent(typeof(Mount))]
	public class MountThrow : NetComponent, IOnPreUpdate, IOnPreSimulate
	{
		public KeyCode throwKey;

		public Mount mount;

		public bool fromRoot = true;

		public bool inheritRBVelocity = true;

		public Vector3 offset = new Vector3(0f, 3f, 0f);

		public Vector3 velocity = new Vector3(0f, 1f, 5f);

		private bool throwQueued;

		public override void OnAwake()
		{
			base.OnAwake();
			if (mount == null)
			{
				mount = GetComponent<Mount>();
			}
		}

		public void OnPreUpdate()
		{
			if (base.IsMine && Input.GetKeyDown(throwKey))
			{
				throwQueued = true;
			}
		}

		public void OnPreSimulate(int frameId, int subFrameId)
		{
			if (!throwQueued)
			{
				return;
			}
			throwQueued = false;
			List<IMountable> mountedObjs = mount.mountedObjs;
			for (int i = 0; i < mountedObjs.Count; i++)
			{
				IMountable mountable = mountedObjs[i];
				Rigidbody rb = mountable.Rb;
				if ((bool)rb && mountable.IsThrowable)
				{
					SyncState syncState = mountable as SyncState;
					if ((bool)syncState)
					{
						Transform transform = (fromRoot ? mount.mountsLookup.transform : base.transform);
						Vector3 position = transform.TransformPoint(offset);
						Quaternion rotation = transform.rotation;
						Vector3 vector = ((inheritRBVelocity && (bool)rb) ? (rb.velocity + transform.TransformVector(velocity)) : transform.TransformVector(velocity));
						syncState.Throw(position, rotation, vector);
					}
				}
			}
		}
	}
}
