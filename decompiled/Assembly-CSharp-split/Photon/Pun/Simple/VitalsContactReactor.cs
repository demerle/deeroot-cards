using System;
using UnityEngine;

namespace Photon.Pun.Simple;

public class VitalsContactReactor : ContactReactorBase, IOnContactEvent, IVitalsContactReactor, IContactReactor, IOnStateChange
{
	[SerializeField]
	[HideInInspector]
	protected VitalNameType vitalNameType = new VitalNameType(VitalType.Health);

	[HideInInspector]
	public double dischargeOnEnter = 20.0;

	[HideInInspector]
	public double dischargeOnExit = 20.0;

	[HideInInspector]
	public double dischargeOnScan = 20.0;

	[SerializeField]
	[HideInInspector]
	protected double dischargePerSec = 20.0;

	[Tooltip("Unconsumed values (remainders) should be passed through to the next vital in the stack of vitals.")]
	public bool propagate;

	public bool allowOverload;

	[SerializeField]
	protected bool isPickup = true;

	public bool useCharges;

	public double _charges = 50.0;

	[Tooltip("The Charges value that will be set on initialization, and whenever this object respawns.")]
	[SerializeField]
	protected double initialCharges = 50.0;

	public Consumption consumeDespawn;

	protected double valuePerFixed;

	public virtual VitalNameType VitalNameType => new VitalNameType(VitalType.None);

	public double DischargePerSec
	{
		get
		{
			return dischargePerSec;
		}
		internal set
		{
			valuePerFixed = value * (double)Time.fixedDeltaTime;
			dischargePerSec = value;
		}
	}

	public virtual bool Propagate
	{
		get
		{
			return propagate;
		}
		set
		{
			propagate = value;
		}
	}

	public virtual bool AllowOverload
	{
		get
		{
			return allowOverload;
		}
		set
		{
			allowOverload = value;
		}
	}

	public override bool IsPickup => isPickup;

	public virtual double Charges => _charges;

	public virtual Consumption ConsumeCharges(double amount)
	{
		if (amount == 0.0)
		{
			return Consumption.None;
		}
		double val = _charges - amount;
		double num = (_charges = ((initialCharges >= 0.0) ? Math.Max(val, 0.0) : Math.Min(val, 0.0)));
		if (num != initialCharges)
		{
			if (num != 0.0)
			{
				return Consumption.Partial;
			}
			return Consumption.All;
		}
		return Consumption.None;
	}

	protected virtual void Consume(Consumption consumed)
	{
		if (consumed != Consumption.None && consumeDespawn != Consumption.None && syncState != null)
		{
			if (consumed == Consumption.All)
			{
				syncState.Despawn(immediate: false);
			}
			else if (consumeDespawn == Consumption.Partial)
			{
				syncState.Despawn(immediate: false);
			}
		}
	}

	public override void OnAwakeInitialize(bool isNetObject)
	{
		base.OnAwakeInitialize(isNetObject);
		valuePerFixed = dischargePerSec * (double)Time.fixedDeltaTime;
	}

	protected override Consumption ProcessContactEvent(ContactEvent contactEvent)
	{
		if (!(contactEvent.contactSystem is IVitalsSystem vitalsSystem))
		{
			return Consumption.None;
		}
		double valueForTriggerType = GetValueForTriggerType(contactEvent.contactType);
		double num = vitalsSystem.Vitals.ApplyCharges(vitalNameType, valueForTriggerType, allowOverload, propagate);
		Consumption consumption;
		if (useCharges)
		{
			consumption = ConsumeCharges(num);
		}
		else
		{
			if (num == 0.0)
			{
				return Consumption.None;
			}
			consumption = ((num != valueForTriggerType) ? Consumption.Partial : Consumption.All);
			Consume(consumption);
		}
		if (isPickup && consumption != Consumption.None)
		{
			Mount mount = vitalsSystem.TryPickup(this, contactEvent);
			if ((bool)mount)
			{
				syncState.HardMount(mount);
			}
		}
		return consumption;
	}

	public void OnStateChange(ObjState newState, ObjState previousState, Transform attachmentTransform, Mount attachTo = null, bool isReady = true)
	{
		if (previousState == ObjState.Despawned && (newState & ObjState.Visible) != ObjState.Despawned)
		{
			_charges = initialCharges;
		}
	}

	public double DischargeValue(ContactType contactType = ContactType.Undefined)
	{
		double num = contactType switch
		{
			ContactType.Enter => dischargeOnEnter, 
			ContactType.Stay => dischargePerSec, 
			ContactType.Exit => dischargeOnExit, 
			ContactType.Hitscan => dischargeOnScan, 
			_ => 0.0, 
		};
		if (useCharges)
		{
			if (num >= 0.0)
			{
				return Math.Min(num, _charges);
			}
			return Math.Max(num, _charges);
		}
		return num;
	}

	protected double GetValueForTriggerType(ContactType collideType)
	{
		return collideType switch
		{
			ContactType.Enter => dischargeOnEnter, 
			ContactType.Stay => valuePerFixed, 
			ContactType.Exit => dischargeOnExit, 
			ContactType.Hitscan => dischargeOnScan, 
			_ => 0.0, 
		};
	}
}
