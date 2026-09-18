using System;
using UnityEngine;

namespace Photon.Pun.Simple
{
	public abstract class VitalsUISrcBase : MonoBehaviour, IOnChangeOwnedVitals
	{
		public enum MonitorSource
		{
			Auto = 0,
			Owned = 1,
			Self = 2,
			GameObject = 3
		}

		[Tooltip("Where this VitalUI will look for Vitals data.")]
		[HideInInspector]
		public MonitorSource monitor;

		[Tooltip("Object that this VitalUI will search for an IVitalsSystem vitals data source.")]
		[HideInInspector]
		[SerializeField]
		public UnityEngine.Object vitalsSource;

		[NonSerialized]
		public Vitals vitals;

		public abstract void OnChangeOwnedVitals(IVitalsSystem added, IVitalsSystem removed);

		protected virtual void Reset()
		{
			ApplyVitalsSource(null);
		}

		public virtual IVitalsSystem ApplyVitalsSource(UnityEngine.Object srcObj)
		{
			if (monitor == MonitorSource.Auto)
			{
				if (srcObj == null)
				{
					srcObj = (UnityEngine.Object)GetComponentInParent<IVitalsSystem>();
					monitor = MonitorSource.Self;
				}
				if (srcObj == null)
				{
					srcObj = (UnityEngine.Object)OwnedIVitals.LastItem;
					monitor = MonitorSource.Owned;
				}
			}
			Component component;
			GameObject gameObject;
			if (monitor == MonitorSource.Owned)
			{
				component = OwnedIVitals.LastItem as Component;
				gameObject = null;
			}
			else if (monitor == MonitorSource.Self)
			{
				gameObject = base.gameObject;
				component = null;
			}
			else
			{
				vitalsSource = srcObj;
				gameObject = srcObj as GameObject;
				component = srcObj as Component;
			}
			IVitalsSystem vitalsSystem;
			if ((bool)gameObject)
			{
				vitalsSystem = FindIVitalComponentOnGameObj(gameObject);
				if (vitalsSystem != null)
				{
					vitalsSource = (vitalsSystem as Component).gameObject;
				}
			}
			else if ((bool)component)
			{
				vitalsSystem = component as IVitalsSystem;
				if (monitor == MonitorSource.GameObject)
				{
					vitalsSource = component.gameObject;
				}
			}
			else
			{
				vitalsSystem = null;
				vitalsSource = null;
			}
			return vitalsSystem;
		}

		private static IVitalsSystem FindIVitalComponentOnGameObj(GameObject go)
		{
			if ((bool)go)
			{
				IVitalsSystem vitalsSystem = go.GetComponentInParent<IVitalsSystem>();
				if (vitalsSystem == null)
				{
					vitalsSystem = go.GetComponentInChildren<IVitalsSystem>();
				}
				return vitalsSystem;
			}
			return null;
		}
	}
}
