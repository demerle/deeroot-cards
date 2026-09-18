using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Compression;
using Photon.Compression.Internal;
using Photon.Pun.Simple.Internal;
using Photon.Realtime;
using Photon.Utilities;
using UnityEngine;

namespace Photon.Pun.Simple
{
	[DisallowMultipleComponent]
	[HelpURL("https://doc.photonengine.com/en-us/pun/current/gameplay/simple/simpleoverview")]
	[RequireComponent(typeof(PhotonView))]
	public class NetObject : MonoBehaviour, IMatchmakingCallbacks, IOnPhotonViewPreNetDestroy, IPhotonViewCallback, IOnPhotonViewOwnerChange, IOnPhotonViewControllerChange, IOnPreUpdate, IOnPreSimulate, IOnPostSimulate, IOnQuantize, IOnIncrementFrame, IOnPreQuit
	{
		private class PackObjRecord
		{
			public Component component;

			public PackObjectDatabase.PackObjectInfo info;

			public PackFrame[] packFrames;

			public FastBitMask128 prevReadyMask;

			public FastBitMask128 readyMask;

			public IPackObjOnReadyChange onReadyCallback;
		}

		[SerializeField]
		[HideInInspector]
		[Tooltip("Enabling this will tell the serializer to completely exclude this net object from serialization if none of its content has changed. While this will remove heartbeat data, It may also produce undesirable extrapolation and buffer resizing behavior, as receiving clients will see this as a network failure.")]
		protected bool skipWhenEmpty;

		[SerializeField]
		[HideInInspector]
		[Tooltip("Controls if incoming NetObject updates from a current non-owner/controller should be ignored. The exception to this is if the controller is currently null/-1, which indicates that the initial ownership messages from the Master has not yet arrived or been applied, in which case the first arriving updates originating Player will be treated as the current Controller, regardless of this setting.")]
		protected bool ignoreNonControllerUpdates = true;

		[SerializeField]
		[HideInInspector]
		[Tooltip("When enabled, if a frame update for this Net Object arrives AFTER that frame number has already been applied (it will have been reconstructed/extrapolated with a best guess), the incoming update will be immediately applied, and all frames between that frame and the current snapshot will be reapplied.")]
		protected bool resimulateLateArrivals = true;

		protected Rigidbody _rigidbody;

		protected Rigidbody2D _rigidbody2D;

		public static NonAllocDictionary<int, NetObject> activeControlledNetObjs = new NonAllocDictionary<int, NetObject>();

		public static NonAllocDictionary<int, NetObject> activeUncontrolledNetObjs = new NonAllocDictionary<int, NetObject>();

		private static Queue<NetObject> pendingActiveNetObjDictChanges = new Queue<NetObject>();

		private static bool netObjDictsLocked;

		[NonSerialized]
		public Dictionary<Component, int> colliderLookup = new Dictionary<Component, int>();

		[NonSerialized]
		public List<Component> indexedColliders = new List<Component>();

		[NonSerialized]
		public int bitsForColliderIndex;

		[NonSerialized]
		public FastBitMask128 frameValidMask;

		[NonSerialized]
		public int[] originHistory;

		[NonSerialized]
		public FastBitMask128 syncObjReadyMask;

		[NonSerialized]
		public FastBitMask128 packObjReadyMask;

		[NonSerialized]
		private readonly Dictionary<Component, int> packObjIndexLookup = new Dictionary<Component, int>();

		private bool _allObjsAreReady;

		protected int viewID;

		[NonSerialized]
		public PhotonView photonView;

		private static List<Component> reusableComponents = new List<Component>();

		private static readonly List<IOnJoinedRoom> onJoinedRoomCallbacks = new List<IOnJoinedRoom>();

		private static readonly List<IOnAwake> onAwakeCallbacks = new List<IOnAwake>();

		private static readonly List<IOnStart> onStartCallbacks = new List<IOnStart>();

		private readonly List<IOnEnable> onEnableCallbacks = new List<IOnEnable>();

		private readonly List<IOnDisable> onDisableCallbacks = new List<IOnDisable>();

		public readonly List<IOnPreUpdate> onPreUpdateCallbacks = new List<IOnPreUpdate>();

		public readonly List<IOnAuthorityChanged> onAuthorityChangedCallbacks = new List<IOnAuthorityChanged>();

		public readonly List<IOnNetSerialize> onNetSerializeCallbacks = new List<IOnNetSerialize>();

		public readonly List<IOnCriticallyLateFrame> onCriticallyLateFrameCallbacks = new List<IOnCriticallyLateFrame>();

		public readonly List<IOnIncrementFrame> onIncrementFramesCallbacks = new List<IOnIncrementFrame>();

		public readonly List<IOnSnapshot> onSnapshotCallbacks = new List<IOnSnapshot>();

		public readonly List<IOnQuantize> onQuantizeCallbacks = new List<IOnQuantize>();

		public readonly List<IOnInterpolate> onInterpolateCallbacks = new List<IOnInterpolate>();

		public readonly List<IOnCaptureState> onCaptureCurrentStateCallbacks = new List<IOnCaptureState>();

		public readonly List<IOnPreSimulate> onPreSimulateCallbacks = new List<IOnPreSimulate>();

		public readonly List<IOnPostSimulate> onPostSimulateCallbacks = new List<IOnPostSimulate>();

		public readonly List<IOnPreQuit> onPreQuitCallbacks = new List<IOnPreQuit>();

		public readonly List<IOnPreNetDestroy> onPreNetDestroyCallbacks = new List<IOnPreNetDestroy>();

		private readonly List<IOnNetObjReady> onNetObjReadyCallbacks = new List<IOnNetObjReady>();

		private readonly List<SyncObject> syncObjects = new List<SyncObject>();

		private readonly List<PackObjRecord> packObjects = new List<PackObjRecord>();

		private bool processedInitialBacklog;

		private float firstDeserializeTime;

		public bool SkipWhenEmpty
		{
			get
			{
				return skipWhenEmpty;
			}
		}

		public bool IgnoreNonControllerUpdates
		{
			get
			{
				return ignoreNonControllerUpdates;
			}
		}

		public bool ResimulateLateArrivals
		{
			get
			{
				return resimulateLateArrivals;
			}
		}

		public Rigidbody Rb
		{
			get
			{
				return _rigidbody;
			}
		}

		public Rigidbody2D Rb2D
		{
			get
			{
				return _rigidbody2D;
			}
		}

		public static bool NetObjDictsLocked
		{
			set
			{
				netObjDictsLocked = value;
				if (!value)
				{
					int i = 0;
					for (int count = pendingActiveNetObjDictChanges.Count; i < count; i++)
					{
						NetObject netObject = pendingActiveNetObjDictChanges.Dequeue();
						netObject.DetermineActiveAndControlled(netObject.photonView.IsMine);
					}
				}
			}
		}

		public bool AllObjsAreReady
		{
			get
			{
				if (!photonView.IsMine)
				{
					return _allObjsAreReady;
				}
				return true;
			}
			private set
			{
				if (_allObjsAreReady != value)
				{
					_allObjsAreReady = value;
					for (int i = 0; i < onNetObjReadyCallbacks.Count; i++)
					{
						onNetObjReadyCallbacks[i].OnNetObjReadyChange(value);
					}
					packObjReadyMask.SetAllTrue();
					syncObjReadyMask.SetAllTrue();
				}
			}
		}

		public int ViewID
		{
			get
			{
				return viewID;
			}
		}

		public void OnSyncObjReadyChange(SyncObject sobj, ReadyStateEnum readyState)
		{
			int syncObjIndex = sobj.SyncObjIndex;
			if (readyState != 0)
			{
				syncObjReadyMask[syncObjIndex] = true;
			}
			else
			{
				syncObjReadyMask[syncObjIndex] = false;
			}
			AllObjsAreReady = syncObjReadyMask.AllAreTrue && packObjReadyMask.AllAreTrue;
		}

		public void OnPackObjReadyChange(Component pobj, ReadyStateEnum readyState)
		{
			int bit = packObjIndexLookup[pobj];
			if (readyState != 0)
			{
				packObjReadyMask[bit] = true;
			}
			else
			{
				packObjReadyMask[bit] = false;
			}
			AllObjsAreReady = syncObjReadyMask.AllAreTrue && packObjReadyMask.AllAreTrue;
		}

		protected void Awake()
		{
			frameValidMask = new FastBitMask128(TickEngineSettings.frameCount);
			originHistory = new int[TickEngineSettings.frameCount];
			int i = 0;
			for (int frameCount = TickEngineSettings.frameCount; i < frameCount; i++)
			{
				originHistory[i] = -1;
			}
			if (!_rigidbody)
			{
				_rigidbody = base.transform.GetNestedComponentInChildren<Rigidbody, NetObject>(true);
			}
			if (!_rigidbody)
			{
				_rigidbody2D = base.transform.GetNestedComponentInChildren<Rigidbody2D, NetObject>(true);
			}
			photonView = GetComponent<PhotonView>();
			if (photonView == null)
			{
				Debug.LogWarning("PhotonView missing from NetObject on GameObject '" + base.name + "'. One will be added to suppress errors, but this object will likely not be networked correctly.");
				photonView = base.gameObject.AddComponent<PhotonView>();
			}
			CollectAndReorderInterfaces();
			this.IndexColliders();
			base.transform.GetNestedComponentsInChildren<IOnAwake, NetObject>(onAwakeCallbacks);
			int j = 0;
			for (int count = onAwakeCallbacks.Count; j < count; j++)
			{
				onAwakeCallbacks[j].OnAwake();
			}
		}

		private void Start()
		{
			base.transform.GetNestedComponentsInChildren<IOnStart, NetObject>(onStartCallbacks);
			int i = 0;
			for (int count = onStartCallbacks.Count; i < count; i++)
			{
				onStartCallbacks[i].OnStart();
			}
			if (PhotonNetwork.IsConnectedAndReady)
			{
				OnChangeAuthority(photonView.IsMine, true);
			}
			viewID = photonView.ViewID;
		}

		private void OnEnable()
		{
			NetMasterCallbacks.RegisterCallbackInterfaces(this);
			PhotonNetwork.AddCallbackTarget(this);
			photonView.AddCallbackTarget(this);
			DetermineActiveAndControlled(photonView.IsMine);
			int i = 0;
			for (int count = onEnableCallbacks.Count; i < count; i++)
			{
				onEnableCallbacks[i].OnPostEnable();
			}
		}

		private void OnDisable()
		{
			PhotonNetwork.RemoveCallbackTarget(this);
			photonView.RemoveCallbackTarget(this);
			NetMasterCallbacks.RegisterCallbackInterfaces(this, false);
			DetermineActiveAndControlled(photonView.IsMine);
			int i = 0;
			for (int count = onDisableCallbacks.Count; i < count; i++)
			{
				onDisableCallbacks[i].OnPostDisable();
			}
		}

		public void OnPreQuit()
		{
			int i = 0;
			int count = onPreQuitCallbacks.Count;
			for (; i < onPreQuitCallbacks.Count; i++)
			{
				onPreQuitCallbacks[i].OnPreQuit();
			}
		}

		public void OnPreNetDestroy(PhotonView rootView)
		{
			NetObject component = rootView.GetComponent<NetObject>();
			if (!(component == null))
			{
				int i = 0;
				for (int count = onPreNetDestroyCallbacks.Count; i < count; i++)
				{
					onPreNetDestroyCallbacks[i].OnPreNetDestroy(component);
				}
			}
		}

		private void OnDestroy()
		{
			if (activeControlledNetObjs.ContainsKey(photonView.ViewID))
			{
				activeControlledNetObjs.Remove(photonView.ViewID);
			}
			if (activeUncontrolledNetObjs.ContainsKey(photonView.ViewID))
			{
				activeUncontrolledNetObjs.Remove(photonView.ViewID);
			}
		}

		public virtual void PrepareForDestroy()
		{
			MountsManager component = GetComponent<MountsManager>();
			if ((bool)component)
			{
				component.UnmountAll();
			}
		}

		public void OnOwnerChange(Photon.Realtime.Player newOwner, Photon.Realtime.Player previousOwner)
		{
			OnChangeAuthority(photonView.IsMine, true);
		}

		public void OnControllerChange(Photon.Realtime.Player newController, Photon.Realtime.Player previousController)
		{
			OnChangeAuthority(photonView.IsMine, true);
		}

		public void OnFriendListUpdate(List<FriendInfo> friendList)
		{
		}

		public void OnCreatedRoom()
		{
		}

		public void OnCreateRoomFailed(short returnCode, string message)
		{
		}

		public void OnJoinedRoom()
		{
			base.transform.GetNestedComponentsInChildren<IOnJoinedRoom, NetObject>(onJoinedRoomCallbacks);
			int i = 0;
			for (int count = onJoinedRoomCallbacks.Count; i < count; i++)
			{
				onJoinedRoomCallbacks[i].OnJoinedRoom();
			}
			OnChangeAuthority(photonView.IsMine, true);
		}

		public void OnJoinRoomFailed(short returnCode, string message)
		{
		}

		public void OnJoinRandomFailed(short returnCode, string message)
		{
		}

		public void OnLeftRoom()
		{
		}

		private void DetermineActiveAndControlled(bool amController)
		{
			int key = photonView.ViewID;
			if (netObjDictsLocked)
			{
				pendingActiveNetObjDictChanges.Enqueue(this);
				return;
			}
			bool flag = activeControlledNetObjs.ContainsKey(key);
			bool flag2 = activeUncontrolledNetObjs.ContainsKey(key);
			if (base.isActiveAndEnabled)
			{
				if (amController)
				{
					if (!flag)
					{
						activeControlledNetObjs.Add(key, this);
					}
					if (flag2)
					{
						activeUncontrolledNetObjs.Remove(key);
					}
				}
				else
				{
					if (flag)
					{
						activeControlledNetObjs.Remove(key);
					}
					if (!flag2)
					{
						activeUncontrolledNetObjs.Add(key, this);
					}
				}
			}
			else
			{
				if (flag)
				{
					activeControlledNetObjs.Remove(key);
				}
				if (flag2)
				{
					activeUncontrolledNetObjs.Remove(key);
				}
			}
		}

		public void OnChangeAuthority(bool isMine, bool controllerHasChanged)
		{
			DetermineActiveAndControlled(isMine);
			int i = 0;
			for (int count = onAuthorityChangedCallbacks.Count; i < count; i++)
			{
				onAuthorityChangedCallbacks[i].OnAuthorityChanged(isMine, controllerHasChanged);
			}
			if (isMine)
			{
				AllObjsAreReady = true;
			}
		}

		private void CollectAndReorderInterfaces()
		{
			base.transform.GetNestedComponentsInChildren<Component, NetObject>(reusableComponents);
			int i = 0;
			int count = reusableComponents.Count;
			for (; i <= 24; i++)
			{
				for (int j = 0; j < count; j++)
				{
					Component component = reusableComponents[j];
					if (component == this)
					{
						continue;
					}
					IApplyOrder applyOrder = component as IApplyOrder;
					if (applyOrder == null)
					{
						if (i == 13)
						{
							AddInterfaces(component);
							AddPackObjects(component);
						}
					}
					else if (applyOrder.ApplyOrder == i)
					{
						AddInterfaces(component);
					}
				}
			}
			syncObjReadyMask = new FastBitMask128(syncObjects.Count);
			packObjReadyMask = new FastBitMask128(packObjects.Count);
			for (int k = 0; k < syncObjects.Count; k++)
			{
				SyncObject syncObject = syncObjects[k];
				syncObject.SyncObjIndex = k;
				OnSyncObjReadyChange(syncObject, syncObject.ReadyState);
			}
		}

		public void RemoveInterfaces(Component comp)
		{
			AddInterfaces(comp, true);
		}

		private void AddInterfaces(Component comp, bool remove = false)
		{
			AddInterfaceToList(comp, onEnableCallbacks, remove);
			AddInterfaceToList(comp, onDisableCallbacks, remove);
			AddInterfaceToList(comp, onPreUpdateCallbacks, remove);
			AddInterfaceToList(comp, onAuthorityChangedCallbacks, remove);
			AddInterfaceToList(comp, onCaptureCurrentStateCallbacks, remove);
			AddInterfaceToList(comp, onNetSerializeCallbacks, remove, true);
			AddInterfaceToList(comp, onQuantizeCallbacks, remove, true);
			AddInterfaceToList(comp, onIncrementFramesCallbacks, remove, true);
			AddInterfaceToList(comp, onSnapshotCallbacks, remove, true);
			AddInterfaceToList(comp, onCriticallyLateFrameCallbacks, remove, true);
			AddInterfaceToList(comp, onInterpolateCallbacks, remove, true);
			AddInterfaceToList(comp, onPreSimulateCallbacks, remove);
			AddInterfaceToList(comp, onPostSimulateCallbacks, remove);
			AddInterfaceToList(comp, onPreQuitCallbacks, remove);
			AddInterfaceToList(comp, onPreNetDestroyCallbacks, remove);
			AddInterfaceToList(comp, onNetObjReadyCallbacks, remove);
			AddInterfaceToList(comp, syncObjects, remove);
		}

		private void AddInterfaceToList<T>(object comp, List<T> list, bool remove, bool checkSerializationOptional = false) where T : class
		{
			T val = comp as T;
			if (val == null)
			{
				return;
			}
			if (checkSerializationOptional)
			{
				ISerializationOptional serializationOptional = val as ISerializationOptional;
				if (serializationOptional != null && !serializationOptional.IncludeInSerialization)
				{
					return;
				}
			}
			T item = comp as T;
			if (remove && list.Contains(item))
			{
				list.Remove(item);
			}
			else
			{
				list.Add(item);
			}
		}

		private void AddPackObjects(Component comp)
		{
			if (comp == null)
			{
				return;
			}
			Type type = comp.GetType();
			if (comp.GetType().GetCustomAttributes(typeof(PackObjectAttribute), false).Length != 0)
			{
				PackObjectDatabase.PackObjectInfo packObjectInfo = PackObjectDatabase.GetPackObjectInfo(type);
				if (packObjectInfo != null)
				{
					PackObjRecord item = new PackObjRecord
					{
						component = comp,
						onReadyCallback = (comp as IPackObjOnReadyChange),
						info = packObjectInfo,
						packFrames = packObjectInfo.FactoryFramesObj(comp, TickEngineSettings.frameCount),
						prevReadyMask = new FastBitMask128(packObjectInfo.fieldCount),
						readyMask = new FastBitMask128(packObjectInfo.fieldCount)
					};
					packObjIndexLookup.Add(comp, packObjects.Count);
					packObjects.Add(item);
				}
			}
		}

		public void OnPreUpdate()
		{
			int i = 0;
			for (int count = onPreUpdateCallbacks.Count; i < count; i++)
			{
				onPreUpdateCallbacks[i].OnPreUpdate();
			}
		}

		public SerializationFlags GenerateMessage(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{
			OnCaptureCurrentState(frameId);
			OnQuantize(frameId);
			return OnSerialize(frameId, buffer, ref bitposition, writeFlags);
		}

		public SerializationFlags OnSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{
			SerializationFlags serializationFlags = writeFlags;
			int num = ((frameId == 0) ? TickEngineSettings.frameCount : frameId) - 1;
			int count = packObjects.Count;
			for (int i = 0; i < count; i++)
			{
				PackObjRecord packObjRecord = packObjects[i];
				PackFrame packFrame = packObjRecord.packFrames[frameId];
				int fieldCount = packObjRecord.info.fieldCount;
				int bitposition2 = bitposition;
				bitposition += fieldCount;
				int maskOffset = 0;
				serializationFlags |= packObjRecord.info.PackFrameToBuffer(packFrame, packObjRecord.packFrames[num], ref packFrame.mask, ref maskOffset, buffer, ref bitposition, frameId, writeFlags);
				for (int j = 0; j < fieldCount; j++)
				{
					buffer.WriteBool(packFrame.mask[j], ref bitposition2);
				}
			}
			int k = 0;
			for (int count2 = onNetSerializeCallbacks.Count; k < count2; k++)
			{
				serializationFlags |= onNetSerializeCallbacks[k].OnNetSerialize(frameId, buffer, ref bitposition, writeFlags);
			}
			return serializationFlags;
		}

		public void OnDeserialize(int connId, int originFrameId, byte[] buffer, ref int bitposition, bool hasData, FrameArrival arrival)
		{
			if (!hasData)
			{
				return;
			}
			frameValidMask[originFrameId] = true;
			originHistory[originFrameId] = connId;
			int count = packObjects.Count;
			for (int i = 0; i < count; i++)
			{
				PackObjRecord packObjRecord = packObjects[i];
				PackFrame packFrame = packObjRecord.packFrames[originFrameId];
				int fieldCount = packObjRecord.info.fieldCount;
				for (int j = 0; j < fieldCount; j++)
				{
					packFrame.mask[j] = buffer.ReadBool(ref bitposition);
				}
				int maskOffset = 0;
				SerializationFlags serializationFlags = packObjRecord.info.UnpackFrameFromBuffer(packFrame, ref packFrame.mask, ref packFrame.isCompleteMask, ref maskOffset, buffer, ref bitposition, originFrameId, SerializationFlags.None);
				if (arrival >= FrameArrival.IsSnap && !packObjRecord.readyMask.AllAreTrue && (serializationFlags & SerializationFlags.IsComplete) != 0)
				{
					packObjRecord.readyMask.OR(packObjRecord.info.defaultReadyMask);
					packObjRecord.readyMask.OR(packFrame.isCompleteMask);
					FastBitMask128 mask = !packObjRecord.readyMask & packFrame.mask;
					maskOffset = 0;
					packObjRecord.info.CopyFrameToObj(packFrame, packObjRecord.component, ref mask, ref maskOffset);
					BroadcastReadyMaskChange(packObjRecord);
				}
			}
			int k = 0;
			for (int count2 = onNetSerializeCallbacks.Count; k < count2; k++)
			{
				onNetSerializeCallbacks[k].OnNetDeserialize(originFrameId, buffer, ref bitposition, arrival);
			}
			if (!resimulateLateArrivals || arrival < FrameArrival.IsSnap)
			{
				return;
			}
			int frameCount = TickEngineSettings.frameCount;
			int num = originFrameId + 1;
			if (num >= frameCount)
			{
				num -= frameCount;
			}
			int num2 = originFrameId;
			int num3 = originFrameId - 1;
			if (num3 < 0)
			{
				num3 += frameCount;
			}
			for (int l = 0; l <= (int)arrival; l++)
			{
				int m = 0;
				for (int count3 = onSnapshotCallbacks.Count; m < count3; m++)
				{
					bool prevIsValid = frameValidMask[num3];
					bool snapIsValid = frameValidMask[num2];
					bool targIsValid = frameValidMask[num];
					onSnapshotCallbacks[m].OnSnapshot(num3, num2, num, prevIsValid, snapIsValid, targIsValid);
				}
				if (l == (int)arrival)
				{
					break;
				}
				num3 = num2;
				num2 = num;
				num++;
				if (num >= frameCount)
				{
					num -= frameCount;
				}
			}
			int n = 0;
			for (int count4 = onCriticallyLateFrameCallbacks.Count; n < count4; n++)
			{
				onCriticallyLateFrameCallbacks[n].HandleCriticallyLateFrame(originFrameId);
			}
		}

		public void OnPreSimulate(int frameId, int _currSubFrameId)
		{
			int i = 0;
			for (int count = onPreSimulateCallbacks.Count; i < count; i++)
			{
				IOnPreSimulate onPreSimulate = onPreSimulateCallbacks[i];
				Behaviour behaviour = onPreSimulate as Behaviour;
				if (behaviour.enabled && behaviour.gameObject.activeInHierarchy)
				{
					onPreSimulate.OnPreSimulate(frameId, _currSubFrameId);
				}
			}
		}

		public void OnPostSimulate(int frameId, int subFrameId, bool isNetTick)
		{
			int i = 0;
			for (int count = onPostSimulateCallbacks.Count; i < count; i++)
			{
				IOnPostSimulate onPostSimulate = onPostSimulateCallbacks[i];
				Behaviour behaviour = onPostSimulate as Behaviour;
				if (behaviour.enabled && behaviour.gameObject.activeInHierarchy)
				{
					onPostSimulate.OnPostSimulate(frameId, subFrameId, isNetTick);
				}
			}
		}

		public SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{
			if (photonView.Group != 0)
			{
				buffer = NetMsgSends.reusableNetObjBuffer;
				int bitposition2 = 0;
				buffer.Write((uint)frameId, ref bitposition2, TickEngineSettings.frameCountBits);
				buffer.WritePackedBytes((uint)viewID, ref bitposition2, 32);
				int num = bitposition2;
				buffer.WriteBool(true, ref bitposition2);
				int bitposition3 = bitposition2;
				bitposition2 += 16;
				SerializationFlags serializationFlags = GenerateMessage(frameId, buffer, ref bitposition2, writeFlags);
				if (serializationFlags == SerializationFlags.None)
				{
					if (skipWhenEmpty)
					{
						return SerializationFlags.None;
					}
					bitposition2 = num;
					buffer.WriteBool(false, ref bitposition);
				}
				if (serializationFlags != 0 || !SkipWhenEmpty)
				{
					buffer.Write((uint)(bitposition2 - bitposition3), ref bitposition3, 16);
					buffer.WritePackedBytes(0uL, ref bitposition2, 32);
					buffer.Send(bitposition2, base.gameObject, serializationFlags);
				}
				return SerializationFlags.None;
			}
			return GenerateMessage(frameId, buffer, ref bitposition, writeFlags);
		}

		public void OnCaptureCurrentState(int frameId)
		{
			int count = packObjects.Count;
			for (int i = 0; i < count; i++)
			{
				PackObjRecord packObjRecord = packObjects[i];
				packObjRecord.info.CaptureObj(packObjRecord.component, packObjRecord.packFrames[frameId]);
			}
			int j = 0;
			for (int count2 = onCaptureCurrentStateCallbacks.Count; j < count2; j++)
			{
				onCaptureCurrentStateCallbacks[j].OnCaptureCurrentState(frameId);
			}
		}

		public void OnQuantize(int frameId)
		{
			int i = 0;
			for (int count = onQuantizeCallbacks.Count; i < count; i++)
			{
				IOnQuantize onQuantize = onQuantizeCallbacks[i];
				Behaviour behaviour = onQuantize as Behaviour;
				if (behaviour.enabled && behaviour.gameObject.activeInHierarchy)
				{
					onQuantize.OnQuantize(frameId);
				}
			}
		}

		public void OnIncrementFrame(int newFrameId, int newSubFrameId, int previousFrameId, int prevSubFrameId)
		{
			int i = 0;
			for (int count = onIncrementFramesCallbacks.Count; i < count; i++)
			{
				onIncrementFramesCallbacks[i].OnIncrementFrame(newFrameId, newSubFrameId, previousFrameId, prevSubFrameId);
			}
		}

		public bool OnSnapshot(int localTargFrameId)
		{
			if (!photonView)
			{
				return false;
			}
			if (!photonView.enabled)
			{
				return false;
			}
			ConnectionTickOffsets value;
			if (!TickManager.perConnOffsets.TryGetValue(photonView.ControllerActorNr, out value))
			{
				return false;
			}
			if (value == null)
			{
				return false;
			}
			if (!value.hadInitialSnapshot)
			{
				return false;
			}
			int advanceCount = value.advanceCount;
			if (advanceCount == 0)
			{
				return false;
			}
			int frameCount = TickEngineSettings.frameCount;
			int num = value.ConvertFrameLocalToOrigin(localTargFrameId);
			int num2 = num - 1;
			if (num2 < 0)
			{
				num2 += frameCount;
			}
			int num3 = num - 2;
			if (num3 < 0)
			{
				num3 += frameCount;
			}
			int frameCount2 = TickEngineSettings.frameCount;
			int i = 0;
			int num4 = num;
			for (; i < advanceCount; i++)
			{
				num4 = num + i;
				if (num4 >= frameCount2)
				{
					num4 -= frameCount2;
				}
				int num5 = num4 - TickEngineSettings.halfFrameCount;
				if (num5 < 0)
				{
					num5 += frameCount2;
				}
				bool flag = frameValidMask[num2];
				bool flag2 = frameValidMask[num4];
				int count = packObjects.Count;
				for (int j = 0; j < count; j++)
				{
					PackObjRecord packObjRecord = packObjects[j];
					PackFrame packFrame = packObjRecord.packFrames[num2];
					PackFrame packFrame2 = packObjRecord.packFrames[num4];
					packObjRecord.readyMask.OR(packObjRecord.info.defaultReadyMask);
					packObjRecord.readyMask.OR(packFrame.isCompleteMask);
					if (!flag2)
					{
						packObjRecord.info.CopyFrameToFrame(packFrame, packFrame2);
					}
					int maskOffset = 0;
					packObjRecord.info.SnapObject(packFrame, packFrame2, packObjRecord.component, ref packObjRecord.readyMask, ref maskOffset);
					if (flag)
					{
						maskOffset = 0;
						packObjRecord.info.CopyFrameToObj(packFrame, packObjRecord.component, ref packFrame.mask, ref maskOffset);
					}
					if (!packObjRecord.readyMask.Compare(packObjRecord.prevReadyMask))
					{
						BroadcastReadyMaskChange(packObjRecord);
					}
				}
				bool prevIsValid = frameValidMask[num3];
				bool snapIsValid = frameValidMask[num2];
				bool targIsValid = frameValidMask[num4];
				int k = 0;
				for (int count2 = onSnapshotCallbacks.Count; k < count2; k++)
				{
					onSnapshotCallbacks[k].OnSnapshot(num3, num2, num4, prevIsValid, snapIsValid, targIsValid);
				}
				num3 = num2;
				num2 = num4;
				frameValidMask[num5] = false;
			}
			return true;
		}

		private void BroadcastReadyMaskChange(PackObjRecord p)
		{
			OnPackObjReadyChange(p.component, p.readyMask.AllAreTrue ? ReadyStateEnum.Ready : ReadyStateEnum.Unready);
			IPackObjOnReadyChange onReadyCallback = p.onReadyCallback;
			if (onReadyCallback != null)
			{
				onReadyCallback.OnPackObjReadyChange(p.readyMask, p.readyMask.AllAreTrue);
			}
			p.prevReadyMask.Copy(p.readyMask);
		}

		public bool OnInterpolate(int localSnapFrameId, int localTargFrameId, float t)
		{
			ConnectionTickOffsets value;
			if (!TickManager.perConnOffsets.TryGetValue(photonView.ControllerActorNr, out value))
			{
				return false;
			}
			if (value == null)
			{
				return false;
			}
			if (!value.hadInitialSnapshot)
			{
				return false;
			}
			int num = value.ConvertFrameLocalToOrigin(localSnapFrameId);
			int num2 = value.ConvertFrameLocalToOrigin(localTargFrameId);
			if (value.validFrameMask[num2])
			{
				int count = packObjects.Count;
				for (int i = 0; i < count; i++)
				{
					PackObjRecord packObjRecord = packObjects[i];
					PackFrame start = packObjRecord.packFrames[num];
					PackFrame end = packObjRecord.packFrames[num2];
					int maskOffset = 0;
					packObjRecord.info.InterpFrameToObj(start, end, packObjRecord.component, t, ref packObjRecord.readyMask, ref maskOffset);
				}
			}
			int j = 0;
			for (int count2 = onInterpolateCallbacks.Count; j < count2; j++)
			{
				IOnInterpolate onInterpolate = onInterpolateCallbacks[j];
				Behaviour behaviour = onInterpolate as Behaviour;
				if (behaviour.enabled && behaviour.gameObject.activeInHierarchy)
				{
					onInterpolate.OnInterpolate(num, num2, t);
				}
			}
			return true;
		}
	}
}
