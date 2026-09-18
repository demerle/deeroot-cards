using System;
using System.Collections.Generic;
using Photon.Compression;
using Photon.Realtime;
using Photon.Utilities;
using UnityEngine;

namespace Photon.Pun.Simple
{
	[DisallowMultipleComponent]
	public class SyncState : SyncObject<SyncState.Frame>, IMountable, IOnCaptureState, IOnNetSerialize, IOnSnapshot, IReadyable, IOnNetObjReady, IUseKeyframes
	{
		public class Frame : FrameBase
		{
			public enum Changes
			{
				None = 0,
				MountIdChange = 1
			}

			public ObjState state;

			public int? mountToViewID;

			public int? mountTypeId;

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
				Frame frame = sourceFrame as Frame;
				state = frame.state;
				mountToViewID = frame.mountToViewID;
				mountTypeId = frame.mountTypeId;
			}

			public override void Clear()
			{
				base.Clear();
				state = ObjState.Despawned;
				mountToViewID = null;
				mountTypeId = null;
			}

			public bool Compare(Frame otherFrame)
			{
				if (state != otherFrame.state || mountToViewID != otherFrame.mountToViewID || mountTypeId != otherFrame.mountTypeId)
				{
					return false;
				}
				return true;
			}
		}

		[EnumMask(true, typeof(ObjStateEditor))]
		public ObjState initialState;

		[EnumMask(true, typeof(ObjStateEditor))]
		public ObjState respawnState = ObjState.Visible;

		[EnumMask(true, typeof(ObjStateEditor))]
		public ObjState readyState = ObjState.Visible;

		[EnumMask(true, typeof(ObjStateEditor))]
		public ObjState unreadyState;

		[Tooltip("Mount types this NetObject can be attached to.")]
		public MountMaskSelector mountableTo;

		[Tooltip("Automatically return this object to its starting position and attach to original parent when ObjState changes from Despawned to any other state.")]
		public bool autoReset = true;

		[Tooltip("Automatically will request ownership transfer to the owner of NetObjects this becomes attached to.")]
		public bool autoOwnerChange = true;

		[Tooltip("Parent Mount changes will force the entire update from this client to send Reliable. This ensures a keyframe of parenting and position reaches all clients (which isn't certain with packetloss), but can cause a visible hanging if packetloss is present on the network.")]
		public bool mountReliable = true;

		[NonSerialized]
		protected Frame currentState = new Frame();

		[NonSerialized]
		protected Mount currentMount;

		[NonSerialized]
		protected bool netObjIsReady;

		protected MountsManager mountsLookup;

		protected SyncTransform syncTransform;

		protected SyncOwner syncOwner;

		protected ISpawnController iSpawnController;

		protected Dictionary<int, int> mountTypeIdToIndex = new Dictionary<int, int>();

		protected int[] indexToMountTypeId;

		protected int bitsForMountType;

		protected StateChangeInfo respawnStateInfo;

		[NonSerialized]
		public List<IOnStateChange> onStateChangeCallbacks = new List<IOnStateChange>();

		[NonSerialized]
		public List<IFlagTeleport> flagTeleportCallbacks = new List<IFlagTeleport>();

		protected Queue<StateChangeInfo> stateChangeQueue = new Queue<StateChangeInfo>();

		protected Frame prevSerializedFrame;

		public override int ApplyOrder
		{
			get
			{
				return 5;
			}
		}

		public Mount CurrentMount
		{
			get
			{
				return currentMount;
			}
			set
			{
				currentMount = value;
			}
		}

		public bool IsThrowable
		{
			get
			{
				return true;
			}
		}

		public bool IsDroppable
		{
			get
			{
				return true;
			}
		}

		public Rigidbody Rb
		{
			get
			{
				return netObj.Rb;
			}
		}

		public Rigidbody2D Rb2d
		{
			get
			{
				return netObj.Rb2D;
			}
		}

		public override bool AllowReconstructionOfEmpty
		{
			get
			{
				return false;
			}
		}

		public override void OnAwake()
		{
			base.OnAwake();
			iSpawnController = GetComponent<ISpawnController>();
			syncTransform = GetComponent<SyncTransform>();
			syncOwner = GetComponent<SyncOwner>();
			base.transform.GetNestedComponentsInChildren<IOnStateChange, NetObject>(onStateChangeCallbacks);
			base.transform.GetComponents(flagTeleportCallbacks);
			mountsLookup = netObj.GetComponent<MountsManager>();
		}

		public override void OnStart()
		{
			ChangeState(new StateChangeInfo(initialState, base.transform.parent ? base.transform.parent.GetComponent<Mount>() : null, true));
			base.OnStart();
			respawnStateInfo = new StateChangeInfo(respawnState, base.transform.parent ? base.transform.parent.GetComponent<Mount>() : null, base.transform.localPosition, base.transform.localRotation, null, true);
			int num = mountableTo.mask.CountTrueBits(out indexToMountTypeId, MountSettings.mountTypeCount);
			bitsForMountType = num.GetBitsForMaxValue();
			for (int i = 0; i < num; i++)
			{
				mountTypeIdToIndex.Add(indexToMountTypeId[i], i);
			}
		}

		public override void OnJoinedRoom()
		{
			base.OnJoinedRoom();
			if (iSpawnController == null)
			{
				ChangeState(new StateChangeInfo(initialState, base.transform.parent ? base.transform.parent.GetComponent<Mount>() : null, true));
			}
		}

		public override void OnAuthorityChanged(bool isMine, bool controllerChanged)
		{
			base.OnAuthorityChanged(isMine, controllerChanged);
			stateChangeQueue.Clear();
			prevSerializedFrame = null;
		}

		public virtual void OnNetObjReadyChange(bool ready)
		{
			netObjIsReady = ready;
			if (!base.IsMine || iSpawnController == null || iSpawnController.AllowNetObjectReadyCallback(ready))
			{
				if (ready)
				{
					ChangeState(new StateChangeInfo(readyState, currentMount, true));
				}
				else
				{
					ChangeState(new StateChangeInfo(unreadyState, currentMount, true));
				}
			}
		}

		public void SoftMount(Mount attachTo)
		{
			ObjState itemState = (attachTo ? ((currentState.state & (ObjState)(-49)) | ObjState.Mounted) : (currentState.state & (ObjState)(-3)));
			ChangeState(new StateChangeInfo(itemState, attachTo, false));
		}

		public void HardMount(Mount mountTo)
		{
			ObjState itemState = (mountTo ? ((currentState.state & (ObjState)(-49)) | (ObjState)14) : (currentState.state & (ObjState)(-15)));
			ChangeState(new StateChangeInfo(itemState, mountTo, false));
		}

		public void Spawn()
		{
		}

		public void Respawn(bool immediate)
		{
			if (immediate)
			{
				ChangeState(respawnStateInfo);
			}
			else
			{
				stateChangeQueue.Enqueue(respawnStateInfo);
			}
		}

		public void Despawn(bool immediate)
		{
			if (immediate)
			{
				ChangeState(new StateChangeInfo(ObjState.Despawned, null, true));
			}
			else
			{
				stateChangeQueue.Enqueue(new StateChangeInfo(ObjState.Despawned, null, true));
			}
		}

		public void ImmediateUnmount()
		{
			stateChangeQueue.Clear();
			ChangeState(new StateChangeInfo((ObjState)49, null, true));
		}

		public void Drop(Mount newMount, bool force = false)
		{
			stateChangeQueue.Enqueue(new StateChangeInfo((ObjState)17, newMount, force));
		}

		public void Throw(Vector3 position, Quaternion rotation, Vector3 velocity)
		{
			stateChangeQueue.Enqueue(new StateChangeInfo((ObjState)49, null, position, rotation, velocity, false));
		}

		public void ThrowLocal(Transform origin, Vector3 offset, Vector3 velocity)
		{
			stateChangeQueue.Enqueue(new StateChangeInfo((ObjState)49, null, origin.TransformPoint(offset), origin.TransformPoint(velocity), false));
		}

		public virtual void QueueStateChange(ObjState newState, Mount newMount, bool force)
		{
			stateChangeQueue.Enqueue(new StateChangeInfo(newState, newMount, null, null, force));
		}

		public virtual void QueueStateChange(ObjState newState, Mount newMount, Vector3 offset, Vector3 velocity, bool force)
		{
			stateChangeQueue.Enqueue(new StateChangeInfo(newState, newMount, offset, velocity, force));
		}

		protected virtual void DequeueStateChanges()
		{
			while (stateChangeQueue.Count > 0)
			{
				StateChangeInfo stateChangeInfo = stateChangeQueue.Dequeue();
				ChangeState(stateChangeInfo);
			}
		}

		protected virtual void ChangeState(StateChangeInfo stateChangeInfo)
		{
			if (!base.gameObject)
			{
				Debug.LogWarning(base.name + " has been destroyed. Will not try to change state.");
				return;
			}
			ObjState state = currentState.state;
			Mount mount = currentMount;
			ObjState objState = stateChangeInfo.objState;
			Mount mount2 = stateChangeInfo.mount;
			bool flag;
			if (autoReset && state == ObjState.Despawned && objState != 0 && (objState & ObjState.Anchored) == 0)
			{
				StateChangeInfo stateChangeInfo2 = new StateChangeInfo(respawnStateInfo);
				stateChangeInfo2.objState = stateChangeInfo.objState;
				stateChangeInfo = stateChangeInfo2;
				flag = true;
			}
			else
			{
				flag = false;
			}
			bool force = stateChangeInfo.force;
			bool flag2 = objState != state;
			bool flag3 = mount != mount2;
			Transform parent = base.transform.parent;
			if (!force && !flag2 && !flag3)
			{
				return;
			}
			currentState.state = objState;
			Mount prevMount = currentMount;
			currentMount = mount2;
			bool flag4 = (objState & ObjState.Mounted) != 0;
			bool flag5 = flag4 && (object)mount2 == null;
			if (flag4 && !flag5 && mount2 == null)
			{
				Debug.LogError("Invalid Mount!");
				InvalidMountHandler(objState, mount2, force);
				return;
			}
			if (base.IsMine && (flag3 || flag))
			{
				for (int i = 0; i < flagTeleportCallbacks.Count; i++)
				{
					flagTeleportCallbacks[i].FlagTeleport();
				}
			}
			if (flag3)
			{
				if (flag4)
				{
					currentState.mountToViewID = mount2.ViewID;
					currentState.mountTypeId = mount2.mountType.id;
					base.transform.parent = mount2.transform;
					if ((objState & ObjState.AnchoredPosition) != 0)
					{
						base.transform.localPosition = default(Vector3);
					}
					if ((objState & ObjState.AnchoredRotation) != 0)
					{
						base.transform.localRotation = default(Quaternion);
					}
				}
				else
				{
					currentState.mountToViewID = null;
					currentState.mountTypeId = null;
					base.transform.parent = null;
				}
				if (autoOwnerChange && (bool)mount2 && (object)mount != mount2)
				{
					ChangeOwnerToParentMountsOwner();
				}
				Mount.ChangeMounting(this, prevMount, mount2);
			}
			Vector3? offsetPos = stateChangeInfo.offsetPos;
			Quaternion? offsetRot = stateChangeInfo.offsetRot;
			Vector3? velocity = stateChangeInfo.velocity;
			if (offsetRot.HasValue)
			{
				base.transform.rotation = offsetRot.Value;
			}
			if (offsetPos.HasValue)
			{
				base.transform.position = offsetPos.Value;
			}
			if (velocity.HasValue)
			{
				Rigidbody rb = netObj.Rb;
				if ((bool)rb)
				{
					rb.velocity = velocity.Value;
				}
				else
				{
					Rigidbody2D rb2D = netObj.Rb2D;
					if ((bool)rb2D)
					{
						rb2D.velocity = velocity.Value;
					}
				}
			}
			if (flag3 || flag2 || force)
			{
				for (int j = 0; j < onStateChangeCallbacks.Count; j++)
				{
					onStateChangeCallbacks[j].OnStateChange(objState, state, base.transform, currentMount, netObjIsReady);
				}
			}
		}

		private void ChangeOwnerToParentMountsOwner()
		{
			if (!base.IsMine)
			{
				return;
			}
			if (!syncOwner)
			{
				Debug.LogWarning(base.name + " cannot automatically change owner without a " + typeof(SyncOwner).Name + " component.");
			}
			else
			{
				if (currentMount == null)
				{
					return;
				}
				PhotonView photonView = currentMount.PhotonView;
				if ((bool)photonView)
				{
					Photon.Realtime.Player owner = photonView.Owner;
					int newOwnerId = ((owner != null) ? owner.ActorNumber : 0);
					if (autoOwnerChange)
					{
						syncOwner.TransferOwner(newOwnerId);
					}
				}
				else if (autoOwnerChange)
				{
					GetComponent<SyncOwner>().TransferOwner(0);
				}
			}
		}

		protected virtual void InvalidMountHandler(ObjState newState, Mount newMount, bool force)
		{
			Debug.LogWarning("Invalid Mount Handled!!");
			ChangeState(new StateChangeInfo(ObjState.Visible, null, true));
		}

		public virtual bool ChangeMount(int newMountId)
		{
			if ((object)currentMount == null)
			{
				Debug.LogWarning("'" + base.name + "' is not currently mounted, so we cannot change to a different mount.");
				return false;
			}
			if (((int)mountableTo & (1 << newMountId)) == 0)
			{
				Debug.LogWarning("'" + base.name + "' is trying to switch to a mount '" + MountSettings.GetName(newMountId) + "' , but mount is not set as valid in SyncState.");
				return false;
			}
			Dictionary<int, Mount> mountIdLookup = currentMount.mountsLookup.mountIdLookup;
			if (!mountIdLookup.ContainsKey(newMountId))
			{
				Debug.LogWarning("'" + base.name + "' doesn't contain a mount for '" + MountSettings.GetName(newMountId) + "'.");
				return false;
			}
			Mount mount = mountIdLookup[newMountId];
			ChangeState(new StateChangeInfo(currentState.state, mount, false));
			return true;
		}

		public void OnCaptureCurrentState(int frameId)
		{
			DequeueStateChanges();
			frames[frameId].CopyFrom(currentState);
		}

		public SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{
			if (!base.enabled)
			{
				buffer.WriteBool(false, ref bitposition);
				return SerializationFlags.None;
			}
			Frame frame = frames[frameId];
			bool flag = IsKeyframe(frameId);
			bool flag2 = (writeFlags & SerializationFlags.NewConnection) != 0;
			bool flag3 = prevSerializedFrame == null;
			bool num = flag3 || frame.state != prevSerializedFrame.state;
			bool flag4 = flag3 || frame.mountTypeId != prevSerializedFrame.mountTypeId || frame.mountToViewID != prevSerializedFrame.mountToViewID;
			bool flag5 = flag2 || flag || flag3;
			bool flag6 = num || flag4;
			bool flag7 = flag5 || flag6;
			bool flag8 = flag3 || flag2 || (keyframeRate == 0 && flag7) || (mountReliable && flag4);
			if (!flag7 && !flag8)
			{
				buffer.WriteBool(false, ref bitposition);
				return SerializationFlags.None;
			}
			buffer.WriteBool(true, ref bitposition);
			SerializationFlags serializationFlags = SerializationFlags.HasContent;
			if (flag8)
			{
				serializationFlags |= SerializationFlags.ForceReliable;
			}
			buffer.Write((ulong)frame.state, ref bitposition, 6);
			if ((frame.state & ObjState.Mounted) != 0)
			{
				if (flag4 || flag5)
				{
					if (!flag)
					{
						buffer.Write(1uL, ref bitposition, 1);
					}
					buffer.WritePackedBytes((uint)frame.mountToViewID.Value, ref bitposition, 32);
					if (bitsForMountType > 0)
					{
						int num2 = mountTypeIdToIndex[frame.mountTypeId.Value];
						buffer.Write((uint)num2, ref bitposition, bitsForMountType);
					}
				}
				else if (!flag)
				{
					buffer.Write(0uL, ref bitposition, 1);
				}
			}
			prevSerializedFrame = frame;
			return serializationFlags;
		}

		public SerializationFlags OnNetDeserialize(int originFrameId, byte[] buffer, ref int bitposition, FrameArrival arrival)
		{
			Frame frame = frames[originFrameId];
			if (!buffer.ReadBool(ref bitposition))
			{
				return SerializationFlags.None;
			}
			frame.state = (ObjState)buffer.Read(ref bitposition, 6);
			if ((frame.state & ObjState.Mounted) == 0)
			{
				frame.content = FrameContents.Complete;
				return SerializationFlags.HasContent;
			}
			if (IsKeyframe(originFrameId) || buffer.Read(ref bitposition, 1) == 1)
			{
				if ((frame.state & ObjState.Mounted) != 0)
				{
					frame.mountToViewID = (int)buffer.ReadPackedBytes(ref bitposition, 32);
					if (bitsForMountType > 0)
					{
						int num = (int)buffer.Read(ref bitposition, bitsForMountType);
						int value = indexToMountTypeId[num];
						frame.mountTypeId = value;
					}
					else
					{
						frame.mountTypeId = 0;
					}
				}
				frame.content = FrameContents.Complete;
				return SerializationFlags.HasContent;
			}
			frame.mountToViewID = null;
			frame.mountTypeId = null;
			frame.content = FrameContents.Partial;
			return SerializationFlags.HasContent;
		}

		protected override void ApplySnapshot(Frame snapframe, Frame targframe, bool snapIsValid, bool targIsValid)
		{
			if (snapframe.content == FrameContents.Empty)
			{
				return;
			}
			if (targframe.content == FrameContents.Complete)
			{
				Transform newParent;
				if (targframe.mountToViewID.HasValue)
				{
					Mount mount = GetMount(targframe.mountToViewID, targframe.mountTypeId);
					newParent = (mount ? mount.transform.parent : null);
				}
				else
				{
					newParent = null;
				}
				if ((bool)syncTransform)
				{
					syncTransform.UpdateParent(targframe.state, newParent);
				}
			}
			if (snapframe.content >= FrameContents.Extrapolated)
			{
				ApplyFrame(snapframe);
			}
		}

		private void ApplyFrame(Frame frame)
		{
			ObjState state = frame.state;
			bool num = (state & ObjState.Mounted) != 0;
			bool force = false;
			Mount mount2;
			if (num)
			{
				int? mountToViewID = frame.mountToViewID;
				if (mountToViewID.HasValue)
				{
					int? mountTypeId = frame.mountTypeId;
					Mount mount = GetMount(mountToViewID, mountTypeId);
					if ((object)mount == currentMount && state == currentState.state)
					{
						return;
					}
					if ((bool)mount)
					{
						mount2 = mount;
						base.ReadyState = ReadyStateEnum.Ready;
						force = true;
					}
					else
					{
						mount2 = currentMount;
					}
				}
				else if (currentMount == null)
				{
					mount2 = null;
				}
				else
				{
					base.ReadyState = ReadyStateEnum.Ready;
					mount2 = currentMount;
					force = true;
				}
			}
			else
			{
				base.ReadyState = ReadyStateEnum.Ready;
				force = true;
				mount2 = null;
			}
			ChangeState(new StateChangeInfo(state, mount2, force));
		}

		public static Mount GetMount(int? viewID, int? mountId)
		{
			if (!viewID.HasValue || !mountId.HasValue)
			{
				return null;
			}
			PhotonView photonView = PhotonNetwork.GetPhotonView(viewID.Value);
			MountsManager mountsManager = (photonView ? photonView.GetComponent<MountsManager>() : null);
			if ((bool)mountsManager)
			{
				return mountsManager.mountIdLookup[mountId.Value];
			}
			return null;
		}
	}
}
