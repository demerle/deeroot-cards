using System;
using System.Collections.Generic;
using Photon.Pun.Simple.Internal;
using UnityEngine;

namespace Photon.Pun.Simple;

public static class NetMasterCallbacks
{
	public struct DelayedRegistrationItem
	{
		public object comp;

		public bool register;

		public DelayedRegistrationItem(object comp, bool register)
		{
			this.comp = comp;
			this.register = register;
		}
	}

	public static List<IOnTickPreSerialization> onTickPreSerializations = new List<IOnTickPreSerialization>();

	public static List<IOnPreUpdate> onPreUpdates = new List<IOnPreUpdate>();

	public static List<IOnPostUpdate> onPostUpdates = new List<IOnPostUpdate>();

	public static List<IOnPreLateUpdate> onPreLateUpdates = new List<IOnPreLateUpdate>();

	public static List<IOnPostLateUpdate> onPostLateUpdates = new List<IOnPostLateUpdate>();

	public static List<IOnIncrementFrame> onIncrementFrames = new List<IOnIncrementFrame>();

	public static List<IOnPreSimulate> onPreSimulates = new List<IOnPreSimulate>();

	public static List<IOnPostSimulate> onPostSimulates = new List<IOnPostSimulate>();

	public static List<IOnTickSnapshot> onSnapshots = new List<IOnTickSnapshot>();

	public static List<IOnInterpolate> onInterpolates = new List<IOnInterpolate>();

	public static List<IOnPreQuit> onPreQuits = new List<IOnPreQuit>();

	public static Queue<Action> postCallbackActions = new Queue<Action>();

	public static Queue<Action> postSimulateActions = new Queue<Action>();

	public static Queue<Action> postSerializationActions = new Queue<Action>();

	public static Queue<DelayedRegistrationItem> pendingRegistrations = new Queue<DelayedRegistrationItem>();

	private static bool callbacksLocked;

	public static bool CallbacksLocked
	{
		set
		{
			callbacksLocked = value;
			if (!value)
			{
				while (pendingRegistrations.Count > 0)
				{
					DelayedRegistrationItem delayedRegistrationItem = pendingRegistrations.Dequeue();
					RegisterCallbackInterfaces(delayedRegistrationItem.comp, delayedRegistrationItem.register);
				}
				while (postCallbackActions.Count > 0)
				{
					postCallbackActions.Dequeue()();
				}
			}
		}
	}

	public static void RegisterCallbackInterfaces(object comp, bool register = true, bool delay = false)
	{
		if (delay || callbacksLocked)
		{
			pendingRegistrations.Enqueue(new DelayedRegistrationItem(comp, register));
			return;
		}
		CallbackUtilities.RegisterInterface(onPreUpdates, comp, register);
		CallbackUtilities.RegisterInterface(onPostUpdates, comp, register);
		CallbackUtilities.RegisterInterface(onPreLateUpdates, comp, register);
		CallbackUtilities.RegisterInterface(onPostLateUpdates, comp, register);
		CallbackUtilities.RegisterInterface(onIncrementFrames, comp, register);
		CallbackUtilities.RegisterInterface(onPreSimulates, comp, register);
		CallbackUtilities.RegisterInterface(onPostSimulates, comp, register);
		CallbackUtilities.RegisterInterface(onSnapshots, comp, register);
		CallbackUtilities.RegisterInterface(onInterpolates, comp, register);
		CallbackUtilities.RegisterInterface(onPreQuits, comp, register);
	}

	public static void OnPreQuitCallbacks()
	{
		CallbacksLocked = true;
		int i = 0;
		for (int count = onPreQuits.Count; i < count; i++)
		{
			onPreQuits[i].OnPreQuit();
		}
		CallbacksLocked = false;
	}

	public static void OnPreUpdateCallbacks()
	{
		CallbacksLocked = true;
		int i = 0;
		for (int count = onPreUpdates.Count; i < count; i++)
		{
			onPreUpdates[i].OnPreUpdate();
		}
		CallbacksLocked = false;
	}

	public static void OnInterpolateCallbacks(int _prevFrameId, int _currFrameId, float t)
	{
		CallbacksLocked = true;
		int i = 0;
		for (int count = onInterpolates.Count; i < count; i++)
		{
			onInterpolates[i].OnInterpolate(_prevFrameId, _currFrameId, t);
		}
		CallbacksLocked = false;
	}

	public static void OnPreLateUpdateCallbacks()
	{
		CallbacksLocked = true;
		int i = 0;
		for (int count = onPreLateUpdates.Count; i < count; i++)
		{
			onPreLateUpdates[i].OnPreLateUpdate();
		}
		CallbacksLocked = false;
	}

	public static void OnPostSimulateCallbacks(int _currFrameId, int _currSubFrameId, bool isNetTick)
	{
		CallbacksLocked = true;
		int i = 0;
		for (int count = onPostSimulates.Count; i < count; i++)
		{
			onPostSimulates[i].OnPostSimulate(_currFrameId, _currSubFrameId, isNetTick);
		}
		CallbacksLocked = false;
	}

	public static void OnIncrementFrameCallbacks(int _currFrameId, int _currSubFrameId, int _prevFrameId, int _prevSubFrameId)
	{
		CallbacksLocked = true;
		int i = 0;
		for (int count = onIncrementFrames.Count; i < count; i++)
		{
			onIncrementFrames[i].OnIncrementFrame(_currFrameId, _currSubFrameId, _prevFrameId, _prevSubFrameId);
		}
		CallbacksLocked = false;
	}

	public static void OnSnapshotCallbacks(int _currFrameId)
	{
		CallbacksLocked = true;
		int i = 0;
		for (int count = onSnapshots.Count; i < count; i++)
		{
			onSnapshots[i].OnSnapshot(_currFrameId);
		}
		CallbacksLocked = false;
		while (postSimulateActions.Count > 0)
		{
			postSimulateActions.Dequeue()();
		}
	}

	public static void OnPreSerializeTickCallbacks(int _currFrameId, byte[] buffer, ref int bitposition)
	{
		CallbacksLocked = true;
		int i = 0;
		for (int count = onTickPreSerializations.Count; i < count; i++)
		{
			onTickPreSerializations[i].OnPreSerializeTick(_currFrameId, buffer, ref bitposition);
		}
		CallbacksLocked = false;
	}

	public static void OnPreSimulateCallbacks(int currentFrameId, int currentSubFrameId)
	{
		CallbacksLocked = true;
		int i = 0;
		for (int count = onPreSimulates.Count; i < count; i++)
		{
			IOnPreSimulate onPreSimulate = onPreSimulates[i];
			Behaviour behaviour = onPreSimulate as Behaviour;
			if (!behaviour || (behaviour.isActiveAndEnabled && behaviour.gameObject.activeInHierarchy))
			{
				onPreSimulate.OnPreSimulate(currentFrameId, currentSubFrameId);
			}
		}
		CallbacksLocked = false;
	}

	public static void OnPostUpdateCallbacks()
	{
		CallbacksLocked = true;
		int i = 0;
		for (int count = onPostUpdates.Count; i < count; i++)
		{
			IOnPostUpdate onPostUpdate = onPostUpdates[i];
			Behaviour behaviour = onPostUpdate as Behaviour;
			if (!behaviour || (behaviour.isActiveAndEnabled && behaviour.gameObject.activeInHierarchy))
			{
				onPostUpdate.OnPostUpdate();
			}
		}
		CallbacksLocked = false;
	}

	public static void OnPostLateUpdateCallbacks()
	{
		CallbacksLocked = true;
		int i = 0;
		for (int count = onPostLateUpdates.Count; i < count; i++)
		{
			IOnPostLateUpdate onPostLateUpdate = onPostLateUpdates[i];
			Behaviour behaviour = onPostLateUpdate as Behaviour;
			if (!behaviour || (behaviour.isActiveAndEnabled && behaviour.gameObject.activeInHierarchy))
			{
				onPostLateUpdate.OnPostLateUpdate();
			}
		}
		CallbacksLocked = false;
	}
}
