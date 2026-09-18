using System;
using Photon.Pun.Simple.Internal;
using Photon.Utilities;
using UnityEngine;
using emotitron.Utilities.GUIUtilities;

namespace Photon.Pun.Simple
{
	[HelpURL("https://doc.photonengine.com/en-us/pun/current/gameplay/simple/simplecoresynccomponents")]
	public abstract class SyncObject : NetComponent, IOnEnable, IOnDisable, IApplyOrder
	{
		public int _applyOrder = 13;

		[Tooltip("Every X Net Tick this object will serialize a full update, regardless of having changed or not.")]
		[Range(1f, 12f)]
		[HideInInspector]
		[SerializeField]
		protected int keyframeRate = 1;

		[Tooltip("When enabled, components will be instructed to check for changes and serialize them. When disabled, components will be instructed to ONLY send keyframes.")]
		[HideInInspector]
		[SerializeField]
		protected bool useDeltas = true;

		private const string ALWAYS_READY_TOOLTIP = "When true, the NetObject will not factor this SyncObj's ready state into firing IOnNetObjReady callbacks.";

		[ShowIfInterface(typeof(IReadyable), "When true, the NetObject will not factor this SyncObj's ready state into firing IOnNetObjReady callbacks.")]
		public bool _alwaysReady = true;

		private const string INCLUDE_SERIALIZATION_TOOLTIP = "When false, the NetObject will not serialize this SyncObj's state. This cannot be changed at runtime. Disable this component on the owner to disable sync at runtime instead.";

		[Tooltip("When false, the NetObject will not serialize this SyncObj's state. This cannot be changed at runtime. Disable this component on the owner to disable sync at runtime instead.")]
		[HideInInspector]
		public bool serializeThis = true;

		[NonSerialized]
		protected int syncObjIndex;

		protected ReadyStateEnum _readyState;

		public Action<SyncObject, ReadyStateEnum> onReadyCallbacks;

		public virtual int ApplyOrder
		{
			get
			{
				return _applyOrder;
			}
		}

		public bool UseDeltas
		{
			get
			{
				return useDeltas;
			}
			set
			{
				useDeltas = value;
			}
		}

		public virtual bool AlwaysReady
		{
			get
			{
				return _alwaysReady;
			}
		}

		public virtual bool IncludeInSerialization
		{
			get
			{
				return serializeThis;
			}
		}

		public bool SkipWhenEmpty
		{
			get
			{
				return false;
			}
		}

		public int SyncObjIndex
		{
			get
			{
				return syncObjIndex;
			}
			set
			{
				syncObjIndex = value;
			}
		}

		public ReadyStateEnum ReadyState
		{
			get
			{
				return _readyState;
			}
			set
			{
				if (_readyState != value)
				{
					_readyState = value;
					netObj.OnSyncObjReadyChange(this, _readyState);
					if (onReadyCallbacks != null)
					{
						onReadyCallbacks(this, value);
					}
				}
			}
		}

		public bool IsKeyframe(int frameId)
		{
			if (keyframeRate != 0)
			{
				if (frameId % keyframeRate != 0)
				{
					return frameId == TickEngineSettings.frameCount;
				}
				return true;
			}
			return false;
		}

		public virtual void ResetBuffers()
		{
		}

		public override void OnPostEnable()
		{
			base.OnPostEnable();
		}

		public override void OnAuthorityChanged(bool isMine, bool controllerChanged)
		{
			base.OnAuthorityChanged(isMine, controllerChanged);
			if (controllerChanged)
			{
				IReadyable readyable = this as IReadyable;
				if (readyable == null || readyable.AlwaysReady || _readyState != 0)
				{
					ReadyState = ReadyStateEnum.Ready;
				}
				else if (!base.isActiveAndEnabled)
				{
					ReadyState = ReadyStateEnum.Disabled;
				}
				else
				{
					ReadyState = ReadyStateEnum.Unready;
				}
			}
		}

		public override void OnPostDisable()
		{
			base.OnPostDisable();
			ReadyState = ReadyStateEnum.Disabled;
		}

		public override void OnAwake()
		{
			base.OnAwake();
		}

		protected virtual void OnEnable()
		{
		}

		protected int ConvertSecsToTicks(float seconds)
		{
			return (int)(seconds / (Time.fixedDeltaTime * (float)TickEngineSettings.sendEveryXTick));
		}
	}
	public abstract class SyncObject<TFrame> : SyncObject where TFrame : FrameBase
	{
		[NonSerialized]
		public TFrame[] frames;

		protected TFrame prevFrame;

		protected TFrame snapFrame;

		protected TFrame targFrame;

		protected TFrame offtickFrame;

		protected bool hadInitialSnapshot;

		public virtual bool AllowInterpolation
		{
			get
			{
				return true;
			}
		}

		public virtual bool AllowReconstructionOfEmpty
		{
			get
			{
				return true;
			}
		}

		public virtual bool AllowReconstructionOfPartial
		{
			get
			{
				return true;
			}
		}

		public override void OnAwake()
		{
			if (keyframeRate > TickEngineSettings.MaxKeyframes)
			{
				keyframeRate = TickEngineSettings.MaxKeyframes;
				Debug.LogWarning(base.name + "/" + GetType().Name + " keyframe setting exceeds max allowed for the current " + SettingsScriptableObject<TickEngineSettings>.single.name + ".frameCount setting. Reducing to " + keyframeRate);
			}
			base.OnAwake();
			PopulateFrames();
			offtickFrame = frames[TickEngineSettings.frameCount];
		}

		public override void OnPostDisable()
		{
			base.OnPostDisable();
			hadInitialSnapshot = false;
		}

		public override void OnAuthorityChanged(bool isMine, bool controllerChanged)
		{
			base.OnAuthorityChanged(isMine, controllerChanged);
			if (controllerChanged)
			{
				int controllerActorNr = photonView.ControllerActorNr;
				int[] originHistory = netObj.originHistory;
				int i = 0;
				for (int frameCount = TickEngineSettings.frameCount; i < frameCount; i++)
				{
					if (controllerActorNr != originHistory[i])
					{
						frames[i].Clear();
					}
				}
			}
			hadInitialSnapshot = false;
		}

		protected virtual void PopulateFrames()
		{
			FrameBase.PopulateFrames(ref frames);
		}

		protected virtual void InitialCompleteSnapshot(TFrame frame)
		{
		}

		public virtual bool OnSnapshot(int prevFrameId, int snapFrameId, int targFrameId, bool prevIsValid, bool snapIsValid, bool targIsValid)
		{
			if (!base.enabled)
			{
				return false;
			}
			int frameCount = TickEngineSettings.frameCount;
			int halfFrameCount = TickEngineSettings.halfFrameCount;
			prevFrame = frames[prevFrameId];
			snapFrame = frames[snapFrameId];
			if (targFrameId < 0 || targFrameId >= frameCount)
			{
				Debug.Log("BAD FRAME ID " + targFrameId);
			}
			targFrame = frames[targFrameId];
			int num = snapFrameId + halfFrameCount;
			if (num >= frameCount)
			{
				num -= frameCount;
			}
			frames[num].Clear();
			if (targIsValid)
			{
				switch (targFrame.content)
				{
				case FrameContents.Empty:
					if (AllowReconstructionOfEmpty)
					{
						ReconstructEmptyFrame();
					}
					break;
				case FrameContents.Partial:
					if (AllowReconstructionOfPartial)
					{
						ReconstructIncompleteFrame();
					}
					break;
				case FrameContents.NoChange:
					targFrame.CopyFrom(snapFrame);
					break;
				}
			}
			else if (AllowReconstructionOfEmpty)
			{
				if (snapIsValid || snapFrame.content >= FrameContents.Extrapolated)
				{
					ConstructMissingFrame(prevFrame, snapFrame, targFrame);
				}
				else
				{
					targFrame.content = FrameContents.Empty;
				}
			}
			else
			{
				targFrame.content = FrameContents.Empty;
			}
			if (_readyState != ReadyStateEnum.Ready)
			{
				TFrame val = snapFrame;
				if (val.content == FrameContents.Complete)
				{
					InitialCompleteSnapshot(val);
					base.ReadyState = ReadyStateEnum.Ready;
				}
			}
			ApplySnapshot(snapFrame, targFrame, snapIsValid, targIsValid);
			return true;
		}

		protected virtual void ApplySnapshot(TFrame snapframe, TFrame targframe, bool snapIsValid, bool targIsValid)
		{
		}

		protected virtual void ReconstructEmptyFrame()
		{
			if (snapFrame.content == FrameContents.Extrapolated || snapFrame.content == FrameContents.Complete)
			{
				targFrame.content = FrameContents.Extrapolated;
				targFrame.CopyFrom(snapFrame);
			}
			else
			{
				targFrame.content = FrameContents.Empty;
			}
		}

		protected virtual void ReconstructIncompleteFrame()
		{
			targFrame.CopyFrom(snapFrame);
			targFrame.content = FrameContents.Partial;
		}

		protected virtual void ConstructMissingFrame(TFrame prevFrame, TFrame snapframe, TFrame targframe)
		{
			if (snapframe.content == FrameContents.Empty)
			{
				targframe.content = FrameContents.Empty;
				return;
			}
			ConnectionTickOffsets value;
			if (!TickManager.perConnOffsets.TryGetValue(base.ControllerActorNr, out value))
			{
				Debug.LogError("CONN " + base.ControllerActorNr + " NOT ESTABLISHED IN TICK MANAGER YET.");
			}
			int frameId = targframe.frameId;
			for (int i = 2; i <= 3; i++)
			{
				int num = frameId + i;
				if (num >= TickEngineSettings.frameCount)
				{
					num -= TickEngineSettings.frameCount;
				}
				TFrame val = frames[num];
				if (netObj.frameValidMask[num] && val.content == FrameContents.Complete)
				{
					float t = 1f / (float)i;
					InterpolateFrame(targframe, snapframe, val, t);
					if (targframe.content != 0)
					{
						return;
					}
				}
			}
			targframe.content = FrameContents.Empty;
			ExtrapolateFrame(prevFrame, snapframe, targframe);
		}

		public virtual bool OnInterpolate(int snapFrameId, int targFrameId, float t)
		{
			if (!AllowInterpolation)
			{
				return false;
			}
			if (!base.isActiveAndEnabled)
			{
				return false;
			}
			if (base.IsMine)
			{
				return false;
			}
			return true;
		}

		protected virtual void InterpolateFrame(TFrame targframe, TFrame startframe, TFrame endframe, float t)
		{
			targframe.Clear();
		}

		protected virtual void ExtrapolateFrame(TFrame prevframe, TFrame snapframe, TFrame targframe)
		{
			targframe.Clear();
		}
	}
}
