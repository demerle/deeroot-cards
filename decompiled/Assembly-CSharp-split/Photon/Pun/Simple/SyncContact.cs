using System.Collections.Generic;
using Photon.Compression;
using UnityEngine;

namespace Photon.Pun.Simple;

public class SyncContact : SyncObject<SyncContact.Frame>, ISyncContact, IOnSnapshot, IOnNetSerialize, IOnAuthorityChanged, IOnCaptureState, ISerializationOptional
{
	public struct ContactRecord
	{
		public ContactType contactType;

		public int contactSystemViewID;

		public byte contactSystemIndex;

		public ContactRecord(int contactSystemViewID, byte contactSystemIndex, ContactType contactType)
		{
			this.contactSystemViewID = contactSystemViewID;
			this.contactSystemIndex = contactSystemIndex;
			this.contactType = contactType;
		}

		public override string ToString()
		{
			return string.Concat(contactType, " view: ", contactSystemViewID, " index:", contactSystemIndex);
		}
	}

	public class Frame : FrameBase
	{
		public List<ContactRecord> contactRecords = new List<ContactRecord>(1);

		public Frame()
		{
		}

		public Frame(int frameId)
			: base(frameId)
		{
		}

		public override void CopyFrom(FrameBase sourceFrame)
		{
			base.CopyFrom(sourceFrame);
			content = FrameContents.Empty;
			contactRecords.Clear();
		}

		public static Frame Construct(int frameId)
		{
			return new Frame(frameId);
		}

		public override void Clear()
		{
			base.Clear();
			contactRecords.Clear();
		}
	}

	protected Frame currentState = new Frame();

	protected IContactTrigger contactTrigger;

	protected Rigidbody rb;

	protected Rigidbody2D rb2d;

	protected bool _hasRigidbody;

	protected Queue<ContactEvent> queuedContactEvents = new Queue<ContactEvent>();

	public bool HasRigidbody => _hasRigidbody;

	public GameObject VisiblePickupObj => base.gameObject;

	public override void OnAwake()
	{
		base.OnAwake();
		contactTrigger = GetComponent<IContactTrigger>();
		rb = GetComponentInParent<Rigidbody>();
		rb2d = GetComponentInParent<Rigidbody2D>();
		_hasRigidbody = (bool)rb || (bool)rb2d;
	}

	public virtual void SyncContactEvent(ContactEvent contactEvent)
	{
		if (base.IsMine)
		{
			EnqueueEvent(contactEvent);
		}
	}

	protected virtual bool EnqueueEvent(ContactEvent contactEvent)
	{
		queuedContactEvents.Enqueue(contactEvent);
		return true;
	}

	public virtual void OnCaptureCurrentState(int frameId)
	{
		Frame frame = frames[frameId];
		frame.content = FrameContents.Empty;
		while (queuedContactEvents.Count > 0)
		{
			ContactEvent contactEvent = queuedContactEvents.Dequeue();
			Consumption num = Contact(contactEvent);
			if (num != Consumption.None)
			{
				frame.content = FrameContents.Complete;
				List<ContactRecord> contactRecords = frame.contactRecords;
				ContactRecord item = new ContactRecord(contactEvent.contactSystem.ViewID, contactEvent.contactSystem.SystemIndex, contactEvent.contactType);
				contactRecords.Add(item);
			}
			if (num == Consumption.All)
			{
				break;
			}
		}
		queuedContactEvents.Clear();
	}

	protected virtual Consumption Contact(ContactEvent contactEvent)
	{
		return contactTrigger.ContactCallbacks(contactEvent);
	}

	protected virtual void Consume(Frame frame, ContactEvent contactEvent, Consumption consumed)
	{
	}

	public virtual SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
	{
		Frame obj = frames[frameId];
		List<ContactRecord> contactRecords = obj.contactRecords;
		if (obj.content == FrameContents.Complete)
		{
			int i = 0;
			for (int count = contactRecords.Count; i < count; i++)
			{
				ContactRecord contactRecord = contactRecords[i];
				buffer.Write(1uL, ref bitposition, 1);
				buffer.WritePackedBytes((uint)contactRecord.contactSystemViewID, ref bitposition, 32);
				buffer.WritePackedBits(contactRecord.contactSystemIndex, ref bitposition, 8);
				buffer.Write(contactRecord.contactType switch
				{
					ContactType.Stay => 1u, 
					ContactType.Hitscan => 3u, 
					ContactType.Enter => 0u, 
					_ => 2u, 
				}, ref bitposition, 2);
			}
			buffer.Write(0uL, ref bitposition, 1);
			return (SerializationFlags)5;
		}
		buffer.WriteBool(b: false, ref bitposition);
		return SerializationFlags.None;
	}

	public SerializationFlags OnNetDeserialize(int originFrameId, byte[] buffer, ref int bitposition, FrameArrival arrival)
	{
		Frame frame = frames[originFrameId];
		if (buffer.ReadBool(ref bitposition))
		{
			do
			{
				List<ContactRecord> contactRecords = frame.contactRecords;
				ContactRecord item = new ContactRecord((int)buffer.ReadPackedBytes(ref bitposition, 32), (byte)buffer.ReadPackedBits(ref bitposition, 8), (ContactType)(1 << (int)buffer.Read(ref bitposition, 2)));
				contactRecords.Add(item);
			}
			while (buffer.ReadBool(ref bitposition));
			frame.content = FrameContents.Complete;
			return SerializationFlags.HasContent;
		}
		frame.content = FrameContents.Empty;
		return SerializationFlags.None;
	}

	protected override void ApplySnapshot(Frame snapframe, Frame targframe, bool snapIsValid, bool targIsValid)
	{
		base.ApplySnapshot(snapframe, targframe, snapIsValid, targIsValid);
		if (snapframe.content != FrameContents.Complete)
		{
			return;
		}
		List<ContactRecord> contactRecords = snapframe.contactRecords;
		int i = 0;
		for (int count = contactRecords.Count; i < count; i++)
		{
			ContactRecord contactRecord = contactRecords[i];
			PhotonView photonView = PhotonNetwork.GetPhotonView(contactRecord.contactSystemViewID);
			if (!photonView || !photonView.IsMine)
			{
				continue;
			}
			ContactManager component = photonView.GetComponent<ContactManager>();
			if ((bool)component)
			{
				IContactSystem contacting = component.GetContacting(contactRecord.contactSystemIndex);
				ContactEvent contactEvent = new ContactEvent(contacting, contactTrigger, contactRecord.contactType);
				Consumption consumption = Contact(contactEvent);
				if (consumption != Consumption.None)
				{
					Consume(snapframe, contactEvent, consumption);
				}
			}
		}
	}

	protected static int ConvertMaskToIndex(int mask)
	{
		int num = 0;
		if (mask > 32767)
		{
			mask >>= 16;
			num += 16;
		}
		if (mask > 127)
		{
			mask >>= 8;
			num += 8;
		}
		if (mask > 7)
		{
			mask >>= 4;
			num += 4;
		}
		if (mask > 1)
		{
			mask >>= 2;
			num += 2;
		}
		if (mask > 0)
		{
			num++;
		}
		return num;
	}

	public static int ConvertIndexToMask(int index)
	{
		if (index == 0)
		{
			return 0;
		}
		return 1 << index - 1;
	}
}
