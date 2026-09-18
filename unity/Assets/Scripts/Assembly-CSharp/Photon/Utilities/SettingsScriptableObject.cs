using System;
using UnityEngine;

namespace Photon.Utilities
{
	public abstract class SettingsScriptableObject<T> : SettingsScriptableObjectBase where T : SettingsScriptableObjectBase
	{
		public static string AssetName = typeof(T).Name;

		public static Action OnSingletonReady;

		public static T single;

		public static T Single
		{
			get
			{
				if (!(UnityEngine.Object)single)
				{
					single = Resources.Load<T>(AssetName);
					if ((bool)(UnityEngine.Object)single)
					{
						single.Initialize();
					}
				}
				return single;
			}
		}

		protected virtual void Awake()
		{
			T val = single;
			Initialize();
			if ((UnityEngine.Object)val == (UnityEngine.Object)null && (UnityEngine.Object)single != (UnityEngine.Object)null && OnSingletonReady != null)
			{
				OnSingletonReady();
			}
		}

		protected virtual void OnEnable()
		{
			T val = single;
			Initialize();
			if ((UnityEngine.Object)val == (UnityEngine.Object)null && (UnityEngine.Object)single != (UnityEngine.Object)null && OnSingletonReady != null)
			{
				OnSingletonReady();
			}
		}

		public override void Initialize()
		{
			single = this as T;
		}
	}
}
