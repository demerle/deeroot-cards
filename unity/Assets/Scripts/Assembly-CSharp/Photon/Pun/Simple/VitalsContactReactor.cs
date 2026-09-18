using System;
using UnityEngine;

namespace Photon.Pun.Simple
{
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

		public virtual VitalNameType VitalNameType
		{
			get
			{
				return new VitalNameType(VitalType.None);
			}
		}

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

		public override bool IsPickup
		{
			get
			{
				return isPickup;
			}
		}

		public virtual double Charges
		{
			get
			{
				return _charges;
			}
		}

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
			if (consumed != 0 && consumeDespawn != 0 && syncState != null)
			{
				if (consumed == Consumption.All)
				{
					syncState.Despawn(false);
				}
				else if (consumeDespawn == Consumption.Partial)
				{
					syncState.Despawn(false);
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
			IVitalsSystem vitalsSystem = contactEvent.contactSystem as IVitalsSystem;
			if (vitalsSystem == null)
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
			if (isPickup && consumption != 0)
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
			if (previousState == ObjState.Despawned && (newState & ObjState.Visible) != 0)
			{
				_charges = initialCharges;
			}
		}

		public double DischargeValue(ContactType contactType = ContactType.Undefined)
		{
			double num;
			switch (contactType)
			{
			case ContactType.Enter:
				num = dischargeOnEnter;
				break;
			case ContactType.Stay:
				num = dischargePerSec;
				break;
			case ContactType.Exit:
				num = dischargeOnExit;
				break;
			case ContactType.Hitscan:
				num = dischargeOnScan;
				break;
			default:
				num = 0.0;
				break;
			}
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
			switch (collideType)
			{
			case ContactType.Enter:
				return dischargeOnEnter;
			case ContactType.Stay:
				return valuePerFixed;
			case ContactType.Exit:
				return dischargeOnExit;
			case ContactType.Hitscan:
				return dischargeOnScan;
			default:
				return 0.0;
			}
		}
	}
}
