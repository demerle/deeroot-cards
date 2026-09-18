using System;
using System.Collections.Generic;
using UnityEngine;

namespace Photon.Pun.Simple
{
	[Serializable]
	public class Vital
	{
		[SerializeField]
		private VitalDefinition vitalDef;

		[NonSerialized]
		private VitalData vitalData;

		public List<IOnVitalValueChange> OnValueChangeCallbacks = new List<IOnVitalValueChange>(0);

		public List<IOnVitalParamChange> OnParamChangeCallbacks = new List<IOnVitalParamChange>(0);

		public VitalDefinition VitalDef
		{
			get
			{
				return vitalDef;
			}
		}

		public VitalData VitalData
		{
			get
			{
				return vitalData;
			}
			private set
			{
				vitalData = value;
			}
		}

		public double Value
		{
			get
			{
				return vitalData.Value;
			}
			set
			{
				if (value == double.NegativeInfinity)
				{
					return;
				}
				double value2 = vitalData.Value;
				double num = Math.Max(Math.Min(value, vitalDef.MaxValue), 0.0);
				vitalData.Value = num;
				if (value2 != num)
				{
					for (int i = 0; i < OnValueChangeCallbacks.Count; i++)
					{
						OnValueChangeCallbacks[i].OnVitalValueChange(this);
					}
				}
			}
		}

		public int TicksUntilDecay
		{
			get
			{
				return vitalData.ticksUntilDecay;
			}
			set
			{
				vitalData.ticksUntilDecay = value;
			}
		}

		public int TicksUntilRegen
		{
			get
			{
				return vitalData.ticksUntilRegen;
			}
			set
			{
				vitalData.ticksUntilRegen = value;
			}
		}

		public Vital(VitalDefinition vitalDef)
		{
			this.vitalDef = vitalDef;
		}

		public void Initialize(float tickDuration)
		{
			vitalDef.Initialize(tickDuration);
			ResetValues();
		}

		public void ResetValues()
		{
			vitalData = vitalDef.GetDefaultData();
			for (int i = 0; i < OnValueChangeCallbacks.Count; i++)
			{
				OnValueChangeCallbacks[i].OnVitalValueChange(this);
			}
		}

		public void AddIOnVitalChange(IOnVitalChange cb)
		{
			IOnVitalValueChange onVitalValueChange = cb as IOnVitalValueChange;
			if (onVitalValueChange != null)
			{
				OnValueChangeCallbacks.Add(onVitalValueChange);
			}
			IOnVitalParamChange onVitalParamChange = cb as IOnVitalParamChange;
			if (onVitalParamChange != null)
			{
				OnParamChangeCallbacks.Add(onVitalParamChange);
			}
		}

		public void RemoveIOnVitalChange(IOnVitalChange cb)
		{
			IOnVitalValueChange onVitalValueChange = cb as IOnVitalValueChange;
			if (onVitalValueChange != null)
			{
				OnValueChangeCallbacks.Remove(onVitalValueChange);
			}
			IOnVitalParamChange onVitalParamChange = cb as IOnVitalParamChange;
			if (onVitalParamChange != null)
			{
				OnParamChangeCallbacks.Remove(onVitalParamChange);
			}
		}

		public void Apply(VitalData vdata)
		{
			Value = vdata.Value;
			vitalData.ticksUntilDecay = vdata.ticksUntilDecay;
			vitalData.ticksUntilRegen = vdata.ticksUntilRegen;
		}

		public double ApplyCharges(double amt, bool allowOverload, bool ignoreAbsorbtion)
		{
			double num = (ignoreAbsorbtion ? amt : (amt * vitalDef.Absorbtion));
			double value = Value;
			double num2 = value + num;
			if (!allowOverload)
			{
				num2 = Math.Min(num2, vitalDef.FullValue);
			}
			Value = num2;
			return Value - value;
		}

		public bool IsFull(bool allowOverload)
		{
			return Value >= (allowOverload ? vitalDef.MaxValue : vitalDef.FullValue);
		}

		public double ApplyChange(IVitalsContactReactor iVitalsAffector, ContactEvent contectEvent)
		{
			double amount = iVitalsAffector.DischargeValue(contectEvent.contactType);
			return ApplyChange(amount, iVitalsAffector);
		}

		public double ApplyChange(double amount, IVitalsContactReactor reactor = null)
		{
			double value = vitalData.Value;
			double num = vitalData.Value + amount;
			if (reactor != null && reactor.AllowOverload)
			{
				double maxValue = vitalDef.MaxValue;
				if (value >= maxValue)
				{
					return 0.0;
				}
				if (num > maxValue)
				{
					num = maxValue;
				}
			}
			else
			{
				double fullValue = vitalDef.FullValue;
				if (value >= fullValue)
				{
					return 0.0;
				}
				if (num > fullValue)
				{
					num = fullValue;
				}
			}
			Value = num;
			return num - value;
		}

		public void DisruptRegen()
		{
			vitalData.ticksUntilRegen = vitalDef.RegenDelayInTicks;
		}

		public void DisruptDecay()
		{
			vitalData.ticksUntilDecay = vitalDef.DecayDelayInTicks;
		}

		public double TestApplyChange(IVitalsContactReactor iVitalsAffector, ContactEvent contactEvent)
		{
			double charge = iVitalsAffector.DischargeValue(contactEvent.contactType);
			return TestApplyChange(charge, iVitalsAffector);
		}

		public double TestApplyChange(double charge, IVitalsContactReactor iVitalsAffector)
		{
			double value = vitalData.Value;
			double num = vitalData.Value + charge;
			if (iVitalsAffector != null && iVitalsAffector.AllowOverload)
			{
				double maxValue = vitalDef.MaxValue;
				if (value >= maxValue)
				{
					return 0.0;
				}
				if (num > maxValue)
				{
					num = maxValue;
				}
			}
			else
			{
				double maxValue2 = vitalDef.MaxValue;
				if (value >= maxValue2)
				{
					return 0.0;
				}
				if (num > maxValue2)
				{
					num = maxValue2;
				}
			}
			return num - value;
		}

		public void Simulate()
		{
			if (vitalData.ticksUntilRegen > 0)
			{
				vitalData.ticksUntilRegen--;
			}
			else if (vitalData.Value < vitalDef.FullValue)
			{
				Value = Math.Min(vitalData.Value + vitalDef.RegenPerTick, vitalDef.FullValue);
			}
			if (vitalData.ticksUntilDecay > 0)
			{
				vitalData.ticksUntilDecay--;
			}
			else if (vitalData.Value > vitalDef.FullValue)
			{
				Value = Math.Max(vitalData.Value - vitalDef.DecayPerTick, vitalDef.FullValue);
			}
		}
	}
}
