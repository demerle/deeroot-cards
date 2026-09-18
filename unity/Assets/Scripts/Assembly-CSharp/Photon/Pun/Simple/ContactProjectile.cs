using System;
using System.Collections.Generic;
using Photon.Utilities;
using UnityEngine;

namespace Photon.Pun.Simple
{
	public class ContactProjectile : MonoBehaviour, IProjectile, IOnPreSimulate, IOnPreUpdate
	{
		protected IProjectileCannon owner;

		[NonSerialized]
		public Vector3 velocity;

		[NonSerialized]
		public int frameId;

		[NonSerialized]
		public int subFrameId;

		[SerializeField]
		[EnumMask(false, null)]
		protected RespondTo terminateOn = (RespondTo)14;

		[SerializeField]
		[EnumMask(false, null)]
		protected RespondTo damageOn = (RespondTo)14;

		protected Rigidbody rb;

		protected Rigidbody2D rb2d;

		protected bool _hasRigidBody;

		protected bool needsSnapshot;

		protected IContactTrigger localContactTrigger;

		protected bool useRbForces;

		protected bool useRb2dForces;

		protected bool useRBGravity;

		public List<IOnNetworkHit> onHit = new List<IOnNetworkHit>();

		public List<IOnTerminate> onTerminate = new List<IOnTerminate>();

		private Vector3 snapPos;

		private Vector3 targPos;

		private Quaternion snapRot;

		private Quaternion targRot;

		protected static List<NetObject> reusableNetObjects = new List<NetObject>();

		public IProjectileCannon Owner
		{
			get
			{
				return owner;
			}
			set
			{
				owner = value;
			}
		}

		public bool HasRigidbody
		{
			get
			{
				return _hasRigidBody;
			}
		}

		public VitalNameType VitalNameType
		{
			get
			{
				return new VitalNameType(VitalType.None);
			}
		}

		private void Reset()
		{
			localContactTrigger = GetComponent<IContactTrigger>();
			if (localContactTrigger == null)
			{
				localContactTrigger = base.gameObject.AddComponent<ContactTrigger>();
			}
		}

		private void Awake()
		{
			rb = GetComponentInParent<Rigidbody>();
			rb2d = GetComponentInParent<Rigidbody2D>();
			_hasRigidBody = (bool)rb || (bool)rb2d;
			useRbForces = (bool)rb && !rb.isKinematic;
			useRb2dForces = (bool)rb2d && !rb2d.isKinematic;
			useRBGravity = ((bool)rb && rb.useGravity) || ((bool)rb2d && rb.useGravity);
			needsSnapshot = !_hasRigidBody || ((bool)rb && rb.isKinematic) || ((bool)rb2d && rb2d.isKinematic);
			localContactTrigger = GetComponent<IContactTrigger>();
			if (needsSnapshot)
			{
				NetMasterCallbacks.RegisterCallbackInterfaces(this);
			}
			if (_hasRigidBody)
			{
				NetMasterCallbacks.onPreUpdates.Remove(this);
			}
			GetComponents(onHit);
			GetComponents(onTerminate);
		}

		private void OnDestroy()
		{
			NetMasterCallbacks.RegisterCallbackInterfaces(this, false, true);
		}

		public virtual void LagCompensate(float timeshift)
		{
			snapPos = base.transform.position + velocity * timeshift;
			if ((bool)rb && rb.useGravity)
			{
				velocity += Physics.gravity * timeshift;
			}
			else if ((bool)rb2d)
			{
				velocity += Physics.gravity * timeshift;
			}
		}

		public void Initialize(IProjectileCannon owner, int frameId, int subFrameId, Vector3 localVelocity, RespondTo terminateOn, RespondTo damageOn, float timeshift = 0f)
		{
			this.owner = owner;
			velocity = base.transform.TransformDirection(localVelocity);
			this.terminateOn = terminateOn;
			this.damageOn = damageOn;
			this.frameId = frameId;
			this.subFrameId = subFrameId;
			if (timeshift != 0f)
			{
				LagCompensate(timeshift);
			}
			else
			{
				snapPos = base.transform.position;
			}
			if (useRbForces)
			{
				rb.MovePosition(snapPos);
				rb.velocity = velocity;
			}
			else if ((bool)rb2d)
			{
				rb2d.MovePosition(snapPos);
				rb2d.velocity = velocity;
			}
			else
			{
				base.transform.position = snapPos;
				targPos = snapPos + velocity * Time.fixedDeltaTime;
				base.transform.position = snapPos;
			}
			localContactTrigger.Proxy = owner.ContactTrigger;
		}

		public void OnPreSimulate(int frameId, int subFrameId)
		{
			if (!useRbForces && !useRb2dForces)
			{
				SimulateTime(Time.fixedDeltaTime);
			}
		}

		public virtual void SimulateTime(float t)
		{
			Vector3 vector = targPos;
			targPos = vector + velocity * t;
			snapPos = vector;
			if (useRb2dForces)
			{
				velocity += Physics.gravity * rb2d.gravityScale * t;
			}
			else
			{
				velocity += Physics.gravity * t;
			}
			Interpolate(0f);
		}

		public virtual void OnPreUpdate()
		{
			if (!useRbForces && !useRb2dForces)
			{
				Interpolate(NetMaster.NormTimeSinceFixed);
			}
		}

		protected void Interpolate(float t)
		{
			base.transform.position = Vector3.Lerp(snapPos, targPos, t);
		}

		protected virtual void Terminate()
		{
			Debug.Log("Terminate");
			int i = 0;
			for (int count = onTerminate.Count; i < count; i++)
			{
				onTerminate[i].OnTerminate();
			}
			base.gameObject.SetActive(false);
		}
	}
}
