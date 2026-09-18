using Photon.Compression;
using UnityEngine;

namespace Photon.Pun.Simple
{
	public abstract class SyncShootBase : SyncObject<SyncShootBase.Frame>, IOnNetSerialize, IOnPostSimulate, IOnPreUpdate, IOnIncrementFrame, IOnSnapshot, IOnInterpolate
	{
		public class Frame : FrameBase
		{
			public uint triggerMask;

			public Frame()
			{
			}

			public Frame(int frameId)
				: base(frameId)
			{
			}

			public override void CopyFrom(FrameBase sourceFrame)
			{
				triggerMask = 0u;
			}

			public override void Clear()
			{
				base.Clear();
				triggerMask = 0u;
			}
		}

		[Tooltip("Specify the transform hitscans/projectiles will originate from. If null this gameObject will be used as the origin.")]
		[SerializeField]
		protected Transform origin;

		[SerializeField]
		public KeyCode triggerKey;

		protected IContactTrigger contactTrigger;

		protected bool hasSyncContact;

		protected bool triggerQueued;

		public IContactTrigger ContactTrigger
		{
			get
			{
				return contactTrigger;
			}
		}

		public override int ApplyOrder
		{
			get
			{
				return 17;
			}
		}

		public override void OnAwakeInitialize(bool isNetObject)
		{
			base.OnAwakeInitialize(isNetObject);
			contactTrigger = base.transform.GetNestedComponentInParents<IContactTrigger, NetObject>();
			hasSyncContact = contactTrigger.SyncContact != null;
			if (origin == null)
			{
				origin = base.transform;
			}
		}

		public virtual void OnPreUpdate()
		{
			if (base.IsMine && Input.GetKeyDown(triggerKey))
			{
				QueueTrigger();
			}
		}

		public virtual void QueueTrigger()
		{
			if (base.enabled && base.gameObject.activeInHierarchy)
			{
				triggerQueued = true;
			}
		}

		public virtual SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{
			Frame frame = frames[frameId];
			int sendEveryXTick = TickEngineSettings.sendEveryXTick;
			if (frame.triggerMask != 0)
			{
				buffer.WriteBool(true, ref bitposition);
				buffer.Write(frame.triggerMask, ref bitposition, sendEveryXTick);
				return SerializationFlags.HasContent;
			}
			buffer.WriteBool(false, ref bitposition);
			return SerializationFlags.None;
		}

		public virtual SerializationFlags OnNetDeserialize(int originFrameId, byte[] buffer, ref int bitposition, FrameArrival arrival)
		{
			Frame frame = frames[originFrameId];
			int sendEveryXTick = TickEngineSettings.sendEveryXTick;
			if (buffer.ReadBool(ref bitposition))
			{
				frame.triggerMask = buffer.ReadUInt32(ref bitposition, sendEveryXTick);
				frame.content = FrameContents.Complete;
				return SerializationFlags.HasContent;
			}
			frame.triggerMask = 0u;
			frame.content = FrameContents.Empty;
			return SerializationFlags.None;
		}

		public virtual void OnPostSimulate(int frameId, int subFrameId, bool isNetTick)
		{
			if (!base.IsMine)
			{
				return;
			}
			Frame frame = frames[frameId];
			if (subFrameId == 0)
			{
				frame.Clear();
			}
			if (triggerQueued)
			{
				frame.triggerMask |= (uint)(1 << subFrameId);
				if (Trigger(frame, subFrameId))
				{
					TriggerCosmetic(frame, subFrameId);
				}
				triggerQueued = false;
				frame.content = FrameContents.Complete;
			}
		}

		public virtual void OnIncrementFrame(int newFrameId, int newSubFrameId, int previousFrameId, int prevSubFrameId)
		{
			if (!base.IsMine && targFrame != null && targFrame.content == FrameContents.Complete)
			{
				int offset = ((newSubFrameId == 0) ? (TickEngineSettings.sendEveryXTick - 1) : (newSubFrameId - 1));
				ApplySubframe(newFrameId, newSubFrameId, offset);
			}
		}

		protected virtual void ApplySubframe(int newFrameId, int newSubFrameId, int offset)
		{
			if ((targFrame.triggerMask & (1 << offset)) != 0L && Trigger(targFrame, newSubFrameId, NetMaster.RTT))
			{
				TriggerCosmetic(targFrame, newSubFrameId, NetMaster.RTT);
			}
		}

		protected abstract bool Trigger(Frame frame, int subFrameId, float timeshift = 0f);

		protected virtual void TriggerCosmetic(Frame frame, int subFrameId, float timeshift = 0f)
		{
		}
	}
}
