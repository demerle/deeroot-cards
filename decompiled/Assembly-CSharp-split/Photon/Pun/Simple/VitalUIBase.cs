using System;
using UnityEngine;

namespace Photon.Pun.Simple;

public abstract class VitalUIBase : VitalsUISrcBase, IOnVitalValueChange, IOnVitalChange, IOnChangeOwnedVitals
{
	public enum TargetField
	{
		Value,
		Max,
		MaxOverload
	}

	[Tooltip("Which vital type will be monitored.")]
	[HideInInspector]
	public VitalNameType targetVital = new VitalNameType(VitalType.Health);

	[Tooltip("Which value to track. Typically this is value, but can be other VitalDefinition values like Max/Full.")]
	[HideInInspector]
	public TargetField targetField;

	[SerializeField]
	[HideInInspector]
	protected int vitalIndex = -1;

	[NonSerialized]
	protected Vital vital;

	public Vitals Vitals
	{
		get
		{
			return vitals;
		}
		set
		{
			vitals = value;
			if (value == null)
			{
				Vital = null;
			}
			else
			{
				Vital = vitals.GetVital(targetVital);
			}
		}
	}

	public Vital Vital
	{
		get
		{
			return vital;
		}
		private set
		{
			if (vital != null)
			{
				vital.RemoveIOnVitalChange(this);
			}
			value?.AddIOnVitalChange(this);
			vital = value;
			UpdateGraphics(vital);
		}
	}

	public override IVitalsSystem ApplyVitalsSource(UnityEngine.Object vs)
	{
		IVitalsSystem vitalsSystem = base.ApplyVitalsSource(vs);
		Vitals = vitalsSystem?.Vitals;
		vitalIndex = vitalsSystem?.Vitals.GetVitalIndex(targetVital) ?? (-1);
		return vitalsSystem;
	}

	public abstract void Recalculate();

	protected virtual void Awake()
	{
		ApplyVitalsSource(vitalsSource);
		if (monitor == MonitorSource.Auto || monitor == MonitorSource.Owned)
		{
			OwnedIVitals.iOnChangeOwnedVitals.Add(this);
		}
	}

	protected virtual void Start()
	{
		if (vital != null)
		{
			UpdateGraphics(vital);
		}
	}

	protected virtual void OnDestroy()
	{
		if (monitor == MonitorSource.Auto || monitor == MonitorSource.Owned)
		{
			OwnedIVitals.iOnChangeOwnedVitals.Remove(this);
		}
		if (vital != null)
		{
			vital.RemoveIOnVitalChange(this);
		}
	}

	public override void OnChangeOwnedVitals(IVitalsSystem added, IVitalsSystem removed)
	{
		if (added != null)
		{
			vitalsSource = added as Component;
			Vitals = added.Vitals;
		}
		else if (removed.Vitals == vitals)
		{
			IVitalsSystem lastItem = OwnedIVitals.LastItem;
			if (lastItem != null)
			{
				Vitals = lastItem.Vitals;
			}
		}
	}

	public void OnVitalParamChange(Vital vital)
	{
	}

	public virtual void OnVitalValueChange(Vital vital)
	{
		UpdateGraphics(vital);
	}

	public abstract void UpdateGraphics(Vital vital);
}
