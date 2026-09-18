using System;
using System.Collections.Generic;
using Photon.Compression;
using Photon.Pun.Simple.ContactGroups;
using UnityEngine;

namespace Photon.Pun.Simple;

public class SyncVitals : SyncObject<SyncVitals.Frame>, IVitalsSystem, IContactSystem, IOnSnapshot, IOnNetSerialize, IOnAuthorityChanged, IOnPostSimulate, IOnVitalValueChange, IOnVitalChange, IOnCaptureState, IUseKeyframes, IOnStateChange
{
	public class Frame : FrameBase
	{
		public VitalsData vitalsData;

		public Frame()
		{
		}

		public Frame(int frameId)
			: base(frameId)
		{
		}

		public Frame(int frameId, Vitals vitals)
			: base(frameId)
		{
			vitalsData = new VitalsData(vitals);
		}

		public override void CopyFrom(FrameBase sourceFrame)
		{
			base.CopyFrom(sourceFrame);
			VitalsData source = (sourceFrame as Frame).vitalsData;
			vitalsData.CopyFrom(source);
		}
	}

	public Vitals vitals = new Vitals();

	[SerializeField]
	protected ContactGroupMaskSelector contactGroups;

	[Tooltip("Vital triggers/pickups must have this as a valid mount type. When pickups will attach to this mount when picked up.")]
	public MountSelector defaultMounting = new MountSelector(0);

	[Tooltip("When root vital <= zero, syncState.Despawn() will be called. This allows for a default handling of object 'death'.")]
	public bool autoDespawn = true;

	[Tooltip("When OnStateChange changes from ObjState.Despawned to any other state, vital values will be reset to their starting defaults.")]
	public bool resetOnSpawn = true;

	[NonSerialized]
	private VitalsData lastSentData;

	public List<IOnVitalsValueChange> OnVitalsValueChange = new List<IOnVitalsValueChange>(0);

	public List<IOnVitalsParamChange> OnVitalsParamChange = new List<IOnVitalsParamChange>(0);

	public List<IOnRootVitalBecameZero> OnRootVitalBecameZero = new List<IOnRootVitalBecameZero>(0);

	protected SyncState syncState;

	private Vital[] vitalArray;

	protected Vital rootVital;

	private int vitalsCount;

	protected int defaultMountingMask;

	protected bool isPredicted;

	private bool wasDespawned;

	public override int ApplyOrder => 15;

	public override bool AllowReconstructionOfEmpty => false;

	public byte SystemIndex { get; set; }

	public Vitals Vitals => vitals;

	public IContactGroupMask ValidContactGroups => contactGroups;

	public int ValidMountsMask => 1 << defaultMounting.id;

	public Mount DefaultMount { get; set; }

	protected override void PopulateFrames()
	{
		int frameCount = TickEngineSettings.frameCount;
		frames = new Frame[frameCount + 1];
		for (int i = 0; i <= frameCount; i++)
		{
			frames[i] = new Frame(i, vitals);
		}
	}

	public override void OnAwake()
	{
		base.OnAwake();
		base.transform.EnsureRootComponentExists<ContactManager, NetObject>();
		if ((bool)netObj)
		{
			syncState = netObj.GetComponent<SyncState>();
		}
		vitalArray = vitals.VitalArray;
		vitalsCount = vitals.vitalDefs.Count;
		rootVital = vitalArray[0];
		vitals.OnVitalValueChangeCallbacks.Add(this);
		lastSentData = new VitalsData(vitals);
		for (int i = 0; i < vitalsCount; i++)
		{
			vitalArray[i].ResetValues();
		}
		defaultMountingMask = 1 << defaultMounting.id;
	}

	public override void OnStart()
	{
		base.OnStart();
		MountsManager component = GetComponent<MountsManager>();
		if ((bool)component)
		{
			if (component.mountIdLookup.ContainsKey(defaultMounting.id))
			{
				DefaultMount = component.mountIdLookup[defaultMounting.id];
				return;
			}
			Debug.LogWarning("Sync Vitals has a Default Mount setting of " + MountSettings.GetName(defaultMounting.id) + " but no such mount is defined yet on GameObject: '" + base.name + "'. Root mount will be used as a failsafe.");
			defaultMounting.id = 0;
			DefaultMount = component.mountIdLookup[0];
		}
	}

	public override void OnAuthorityChanged(bool isMine, bool controllerChanged)
	{
		base.OnAuthorityChanged(isMine, controllerChanged);
		OwnedIVitals.OnChangeAuthority(this, isMine, controllerChanged);
	}

	public Consumption TryTrigger(IContactReactor icontactReactor, ContactEvent contactEvent, int compatibleMounts)
	{
		if (!(icontactReactor is IVitalsContactReactor vitalsContactReactor))
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
		if (compatibleMounts != defaultMountingMask && (compatibleMounts & defaultMountingMask) == 0)
		{
			return Consumption.None;
		}
		Vital vital = vitals.GetVital(vitalsContactReactor.VitalNameType);
		if (vital == null)
		{
			return Consumption.None;
		}
		double amountConsumed;
		if (base.IsMine)
		{
			double discharge = vitalsContactReactor.DischargeValue(contactEvent.contactType);
			amountConsumed = vitals.ApplyCharges(discharge, vitalsContactReactor.AllowOverload, vitalsContactReactor.Propagate);
		}
		else
		{
			amountConsumed = vital.TestApplyChange(vitalsContactReactor, contactEvent);
		}
		if (icontactReactor is IVitalsConsumable iva)
		{
			return TestConsumption(amountConsumed, iva, contactEvent);
		}
		return Consumption.None;
	}

	protected Consumption TestConsumption(double amountConsumed, IVitalsConsumable iva, ContactEvent contactEvent)
	{
		Consumption consumption = iva.Consumption;
		double num = iva.DischargeValue(contactEvent.contactType);
		switch (consumption)
		{
		case Consumption.None:
			return Consumption.None;
		case Consumption.All:
			if (amountConsumed != 0.0)
			{
				iva.Charges = 0.0;
				return Consumption.All;
			}
			return Consumption.None;
		default:
		{
			int result = ((amountConsumed != 0.0) ? ((num != amountConsumed) ? 1 : 3) : 0);
			iva.Charges -= amountConsumed;
			return (Consumption)result;
		}
		}
	}

	public Mount TryPickup(IContactReactor reactor, ContactEvent contactEvent)
	{
		return DefaultMount;
	}

	public void OnPostSimulate(int frameId, int subFrameId, bool isNetTick)
	{
		if (isNetTick)
		{
			vitals.Simulate();
		}
	}

	public virtual void OnCaptureCurrentState(int frameId)
	{
		VitalData[] datas = frames[frameId].vitalsData.datas;
		for (int i = 0; i < vitalsCount; i++)
		{
			datas[i] = vitalArray[i].VitalData;
		}
	}

	public SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
	{
		if (!base.enabled)
		{
			buffer.WriteBool(b: false, ref bitposition);
			return SerializationFlags.None;
		}
		Frame frame = frames[frameId];
		buffer.WriteBool(b: true, ref bitposition);
		bool keyframe = IsKeyframe(frameId);
		return vitals.Serialize(frame.vitalsData, lastSentData, buffer, ref bitposition, keyframe);
	}

	public SerializationFlags OnNetDeserialize(int originFrameId, byte[] buffer, ref int bitposition, FrameArrival arrival)
	{
		Frame frame = (base.IsMine ? offtickFrame : frames[originFrameId]);
		if (!buffer.ReadBool(ref bitposition))
		{
			return SerializationFlags.None;
		}
		bool keyframe = IsKeyframe(originFrameId);
		SerializationFlags serializationFlags = vitals.Deserialize(frame.vitalsData, buffer, ref bitposition, keyframe);
		frame.content = (((serializationFlags & SerializationFlags.IsComplete) != SerializationFlags.None) ? FrameContents.Complete : (((serializationFlags & SerializationFlags.HasContent) != SerializationFlags.None) ? FrameContents.Partial : FrameContents.Empty));
		return serializationFlags;
	}

	[Obsolete("Use vitals.ApplyCharges() instead")]
	public double ApplyDamage(double damage)
	{
		if (!base.IsMine)
		{
			return damage;
		}
		if (damage == 0.0)
		{
			return damage;
		}
		return vitals.ApplyCharges(damage, allowOverload: false, propagate: true);
	}

	public void OnVitalValueChange(Vital vital)
	{
		if (vital.VitalData.Value <= 0.0)
		{
			RootVitalBecameZero(vital);
		}
		int i = 0;
		for (int count = OnVitalsValueChange.Count; i < count; i++)
		{
			OnVitalsValueChange[i].OnVitalValueChange(vital);
		}
	}

	public void OnVitalParamChange(Vital vital)
	{
		Debug.LogError("Not implemented");
		int i = 0;
		for (int count = OnVitalsParamChange.Count; i < count; i++)
		{
			OnVitalsParamChange[i].OnVitalParamChange(vital);
		}
	}

	protected virtual void RootVitalBecameZero(Vital vital)
	{
		int i = 0;
		for (int count = OnRootVitalBecameZero.Count; i < count; i++)
		{
			OnRootVitalBecameZero[i].OnRootVitalBecameZero(vital, null);
		}
		if (autoDespawn && (bool)syncState && rootVital == vital)
		{
			syncState.Despawn(immediate: false);
		}
	}

	public void OnStateChange(ObjState newState, ObjState previousState, Transform attachmentTransform, Mount attachTo = null, bool isReady = true)
	{
		if (wasDespawned && newState != ObjState.Despawned)
		{
			for (int i = 0; i < vitalsCount; i++)
			{
				vitalArray[i].ResetValues();
			}
		}
		wasDespawned = newState == ObjState.Despawned;
	}

	protected override void ApplySnapshot(Frame snapframe, Frame targframe, bool snapIsValid, bool targIsValid)
	{
		if (snapIsValid && snapframe.content >= FrameContents.Extrapolated)
		{
			vitals.Apply(snapFrame.vitalsData);
		}
	}

	protected override void InterpolateFrame(Frame targframe, Frame startframe, Frame endframe, float t)
	{
		targframe.CopyFrom(startframe);
	}

	protected override void ExtrapolateFrame(Frame prevframe, Frame snapframe, Frame targframe)
	{
		VitalData[] datas = snapframe.vitalsData.datas;
		VitalData[] datas2 = targframe.vitalsData.datas;
		for (int i = 0; i < vitalsCount; i++)
		{
			datas2[i] = vitalArray[i].VitalDef.Extrapolate(datas[i]);
		}
		targframe.content = FrameContents.Extrapolated;
	}
}
