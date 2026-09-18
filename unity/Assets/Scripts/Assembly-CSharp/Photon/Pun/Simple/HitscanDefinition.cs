using System;
using UnityEngine;

namespace Photon.Pun.Simple
{
	[Serializable]
	public class HitscanDefinition
	{
		public Replicate ownerToAuthority = Replicate.Hits;

		public Replicate authorityToAll = Replicate.Hits;

		[Tooltip("This cast/overlap test will be done on the initiating client against colliders on these layers. Exclude layers that won't include any objects that you don't want to test (such as walls).")]
		public LayerMask layerMask = -1;

		public bool useOffset;

		public Vector3 offset1 = new Vector3(0f, 0f, 0f);

		public Vector3 offset2 = new Vector3(0f, 1f, 0f);

		public Vector3 halfExtents = new Vector3(1f, 1f, 1f);

		public Vector3 orientation = new Vector3(0f, 0f, 0f);

		public HitscanType hitscanType;

		public float distance = 100f;

		public float radius = 1f;

		public bool nearestOnly = true;
	}
}
