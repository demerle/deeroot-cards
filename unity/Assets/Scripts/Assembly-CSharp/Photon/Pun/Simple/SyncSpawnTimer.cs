using System;
using Photon.Compression;
using Photon.Utilities;
using UnityEngine;

namespace Photon.Pun.Simple
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(SyncState))]
	public class SyncSpawnTimer : SyncObject<SyncSpawnTimer.Frame>, ISpawnController, ISerializationOptional, IOnNetSerialize, IUseKeyframes, IOnSnapshot, IOnCaptureState, IOnStateChange
	{
		public class Frame : FrameBase
		{
			public int ticksUntilRespawn;

			public int ticksUntilDespawn;

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
				ticksUntilRespawn = frame.ticksUntilRespawn;
				ticksUntilDespawn = frame.ticksUntilDespawn;
			}

			public bool Compare(Frame otherFrame)
			{
				if (ticksUntilRespawn == otherFrame.ticksUntilRespawn)
				{
					return ticksUntilDespawn == otherFrame.ticksUntilDespawn;
				}
				return false;
			}
		}

		[HideInInspector]
		[SerializeField]
		public float initialDelay;

		[HideInInspector]
		[SerializeField]
		public bool respawnEnable = true;

		[EnumMask(true, typeof(ObjStateEditor))]
		[HideInInspector]
		[SerializeField]
		public ObjState despawnOn = ObjState.Mounted;

		[Tooltip("Number of seconds after respawn trigger before respawn occurs.")]
		[HideInInspector]
		[SerializeField]
		public float despawnDelay = 5f;

		[HideInInspector]
		[SerializeField]
		public bool despawnEnable;

		[EnumMask(true, typeof(ObjStateEditor))]
		[HideInInspector]
		[SerializeField]
		public ObjState respawnOn;

		[Tooltip("Number of seconds after respawn trigger before respawn occurs.")]
		[HideInInspector]
		[SerializeField]
		public float respawnDelay = 5f;

		protected SyncState syncState;

		[NonSerialized]
		protected int ticksUntilRespawn = -1;

		[NonSerialized]
		protected int ticksUntilDespawn = -1;

		[NonSerialized]
		protected int spawnWaitAsTicks;

		[NonSerialized]
		protected int respawnWaitAsTicks;

		[NonSerialized]
		protected int despawnWaitAsTicks;

		[NonSerialized]
		protected bool hadInitialSpawn;

		protected int bitsForTicksUntilRespawn;

		protected int bitsForTicksUntilDespawn;

		public override int ApplyOrder
		{
			get
			{
				return 3;
			}
		}

		public override void OnAwake()
		{
			base.OnAwake();
			if ((bool)netObj)
			{
				syncState = netObj.GetComponent<SyncState>();
			}
			spawnWaitAsTicks = ConvertSecsToTicks(initialDelay);
			respawnWaitAsTicks = ConvertSecsToTicks(respawnDelay);
			despawnWaitAsTicks = ConvertSecsToTicks(despawnDelay);
			bitsForTicksUntilRespawn = Math.Max(respawnWaitAsTicks, spawnWaitAsTicks).GetBitsForMaxValue();
			bitsForTicksUntilDespawn = despawnWaitAsTicks.GetBitsForMaxValue();
		}

		public bool AllowNetObjectReadyCallback(bool ready)
		{
			if (ready && spawnWaitAsTicks >= 0)
			{
				return false;
			}
			return true;
		}

		public override void OnJoinedRoom()
		{
			base.OnJoinedRoom();
			if (photonView.IsMine)
			{
				ticksUntilRespawn = spawnWaitAsTicks;
			}
			else
			{
				ticksUntilRespawn = -1;
			}
		}

		public override void OnStart()
		{
			base.OnStart();
			ticksUntilRespawn = spawnWaitAsTicks;
		}

		public void OnStateChange(ObjState newState, ObjState previousState, Transform attachmentTransform, Mount attachTo = null, bool isReady = true)
		{
			if (newState == previousState)
			{
				return;
			}
			if (respawnEnable)
			{
				if (newState == ObjState.Despawned)
				{
					ticksUntilRespawn = respawnWaitAsTicks;
				}
				else if ((previousState & respawnOn) == 0 && (newState & respawnOn) != 0)
				{
					Debug.Log(string.Concat(Time.time, " ", base.name, " ", photonView.OwnerActorNr, " <b>Reset </b> ", previousState, " <> ", newState));
					ticksUntilRespawn = respawnWaitAsTicks;
				}
			}
			if (despawnEnable && (previousState & despawnOn) == 0 && (newState & despawnOn) != 0)
			{
				ticksUntilDespawn = despawnWaitAsTicks;
			}
		}

		public virtual void OnCaptureCurrentState(int frameId)
		{
			Frame frame = frames[frameId];
			if (!hadInitialSpawn || respawnEnable)
			{
				if (ticksUntilRespawn == 0)
				{
					syncState.Respawn(false);
					hadInitialSpawn = true;
				}
				ticksUntilRespawn--;
				frame.ticksUntilRespawn = ticksUntilRespawn;
			}
			if (despawnEnable)
			{
				if (ticksUntilDespawn == 0)
				{
					syncState.Despawn(false);
				}
				ticksUntilDespawn--;
				frame.ticksUntilDespawn = ticksUntilDespawn;
			}
		}

		public virtual SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{
			Frame frame = frames[frameId];
			SerializationFlags serializationFlags = SerializationFlags.None;
			if (!IsKeyframe(frameId))
			{
				return serializationFlags;
			}
			if (respawnEnable)
			{
				int num = frame.ticksUntilRespawn;
				if (num >= 0)
				{
					buffer.WriteBool(true, ref bitposition);
					buffer.Write((ulong)num, ref bitposition, bitsForTicksUntilRespawn);
					serializationFlags |= SerializationFlags.HasContent;
				}
				else
				{
					buffer.WriteBool(false, ref bitposition);
				}
			}
			if (despawnEnable)
			{
				int num2 = frame.ticksUntilDespawn;
				if (num2 >= 0)
				{
					buffer.WriteBool(true, ref bitposition);
					buffer.Write((ulong)num2, ref bitposition, bitsForTicksUntilDespawn);
					serializationFlags |= SerializationFlags.HasContent;
				}
				else
				{
					buffer.WriteBool(false, ref bitposition);
				}
			}
			return serializationFlags;
		}

		public SerializationFlags OnNetDeserialize(int originFrameId, byte[] buffer, ref int bitposition, FrameArrival frameArrival)
		{
			Frame frame = frames[originFrameId];
			SerializationFlags serializationFlags = SerializationFlags.None;
			if (!IsKeyframe(originFrameId))
			{
				frame.content = FrameContents.Empty;
				return serializationFlags;
			}
			if (respawnEnable)
			{
				if (buffer.ReadBool(ref bitposition))
				{
					frame.ticksUntilRespawn = (int)buffer.Read(ref bitposition, bitsForTicksUntilRespawn);
					serializationFlags |= SerializationFlags.HasContent;
				}
				else
				{
					frame.ticksUntilRespawn = -1;
				}
			}
			if (despawnEnable)
			{
				if (buffer.ReadBool(ref bitposition))
				{
					frame.ticksUntilDespawn = (int)buffer.Read(ref bitposition, bitsForTicksUntilDespawn);
					serializationFlags |= SerializationFlags.HasContent;
				}
				else
				{
					frame.ticksUntilDespawn = -1;
				}
			}
			frame.content = FrameContents.Complete;
			return serializationFlags;
		}

		protected override void ApplySnapshot(Frame snapframe, Frame targframe, bool snapIsValid, bool targIsValid)
		{
			if (snapIsValid && snapframe.content > FrameContents.Empty)
			{
				if (respawnEnable)
				{
					ticksUntilRespawn = snapframe.ticksUntilRespawn;
				}
				if (despawnEnable)
				{
					ticksUntilDespawn = snapframe.ticksUntilDespawn;
				}
			}
			else
			{
				if (respawnEnable)
				{
					ticksUntilRespawn--;
					targframe.ticksUntilRespawn = ticksUntilRespawn;
				}
				if (despawnEnable)
				{
					ticksUntilDespawn--;
					targframe.ticksUntilDespawn = ticksUntilDespawn;
				}
			}
			if (photonView.IsMine)
			{
				if (respawnEnable && ticksUntilRespawn == 0)
				{
					syncState.Respawn(false);
				}
				if (despawnEnable && ticksUntilDespawn == 0)
				{
					syncState.Despawn(false);
				}
			}
		}
	}
}
