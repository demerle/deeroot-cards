using System.Collections.Generic;
using Photon.Compression;
using UnityEngine;
using emotitron.Compression;

namespace Photon.Pun.Simple
{
	[DisallowMultipleComponent]
	public class SyncTransform : SyncObject<SyncTransform.Frame>, ISyncTransform, IOnSnapshot, IOnNetSerialize, IOnAuthorityChanged, IReadyable, IUseKeyframes, IDeltaFrameChangeDetect, IOnInterpolate, IOnCaptureState, IFlagTeleport
	{
		public class Frame : FrameBase
		{
			public bool hasTeleported;

			public Matrix m;

			public CompressedMatrix cm;

			public SyncTransform owner;

			public Matrix telem;

			public CompressedMatrix telecm;

			public int parentHash;

			public int telePparentHash;

			public Frame()
			{
				m = new Matrix();
				cm = new CompressedMatrix();
				telem = new Matrix();
				telecm = new CompressedMatrix();
				parentHash = -2;
			}

			public Frame(SyncTransform sst, int frameId)
				: base(frameId)
			{
				m = new Matrix();
				cm = new CompressedMatrix();
				telem = new Matrix();
				telecm = new CompressedMatrix();
				sst.transformCrusher.Capture(sst.transform, cm, m);
				Transform parent = sst.transform.parent;
				parentHash = (parent ? parent.GetInstanceID() : (-1));
			}

			public Frame(Frame srcFrame, int frameId)
				: base(frameId)
			{
				m = new Matrix();
				cm = new CompressedMatrix();
				telem = new Matrix();
				telecm = new CompressedMatrix();
				CopyFrom(srcFrame);
			}

			public void Set(SyncTransform sst, int frameId)
			{
				sst.transformCrusher.Capture(sst.transform, cm, m);
			}

			public override void CopyFrom(FrameBase sourceFrame)
			{
				base.CopyFrom(sourceFrame);
				Frame frame = sourceFrame as Frame;
				if (frame.hasTeleported)
				{
					m.CopyFrom(frame.telem);
					cm.CopyFrom(frame.telecm);
				}
				else
				{
					m.CopyFrom(frame.m);
					cm.CopyFrom(frame.cm);
				}
				hasTeleported = false;
				parentHash = frame.parentHash;
			}

			public bool FastCompareCompressed(Frame other)
			{
				if (cm.Equals(other.cm))
				{
					return true;
				}
				return false;
			}

			public bool FastCompareUncompressed(Frame other)
			{
				if (m.position == other.m.position && m.rotation == other.m.rotation)
				{
					return m.scale == other.m.scale;
				}
				return false;
			}

			public override void Clear()
			{
				base.Clear();
				hasTeleported = false;
				parentHash = -2;
			}

			public override string ToString()
			{
				return string.Concat("[", frameId, " ", m.position, " / ", m.rotation, "]");
			}
		}

		[Tooltip("How lerping between tick states is achieved. 'Standard' is Linear. 'None' holds the previous state until t = 1. 'Catmull Rom' is experimental.")]
		public Interpolation interpolation = Interpolation.Linear;

		[Tooltip("Percentage of extrapolation from previous values. [0 = No Extrapolation] [.5 = 50% extrapolation] [1 = Undampened]. This allows for gradual slowing down of motion when the buffer runs dry.")]
		[Range(0f, 1f)]
		public float extrapolateRatio = 0.5f;

		protected int extrapolationCount;

		[Tooltip("If the distance delta between snapshots exceeds this amount, object will move to new location without lerping. Set this to zero or less to disable (for some tiny CPU savings). You can manually flag a teleport by setting the HasTeleported property to True.")]
		public float teleportThreshold = 5f;

		private float teleportThresholdSqrMag;

		[Tooltip("Entire tick update from this client (all objects being serialized) will be sent as Reliable when FlagTeleport() has been called.")]
		public bool teleportReliable;

		public Dictionary<int, TransformCrusher> masterSharedCrushers = new Dictionary<int, TransformCrusher>();

		public TransformCrusher transformCrusher = new TransformCrusher
		{
			PosCrusher = new ElementCrusher(TRSType.Position, false)
			{
				hideFieldName = true,
				XCrusher = new FloatCrusher(Axis.X, TRSType.Position, true)
				{
					BitsDeterminedBy = BitsDeterminedBy.HalfFloat,
					AccurateCenter = true
				},
				YCrusher = new FloatCrusher(Axis.Y, TRSType.Position, true)
				{
					BitsDeterminedBy = BitsDeterminedBy.HalfFloat,
					AccurateCenter = true
				},
				ZCrusher = new FloatCrusher(Axis.Z, TRSType.Position, true)
				{
					BitsDeterminedBy = BitsDeterminedBy.HalfFloat,
					AccurateCenter = true
				}
			},
			RotCrusher = new ElementCrusher(TRSType.Quaternion, false)
			{
				hideFieldName = true,
				XCrusher = new FloatCrusher(Axis.X, TRSType.Euler, true)
				{
					Bits = 12,
					AccurateCenter = true
				},
				YCrusher = new FloatCrusher(Axis.Y, TRSType.Euler, true)
				{
					Bits = 12,
					AccurateCenter = true
				},
				ZCrusher = new FloatCrusher(Axis.Z, TRSType.Euler, true)
				{
					Bits = 12,
					AccurateCenter = true
				},
				QCrusher = new QuatCrusher(44, true, false)
			},
			SclCrusher = new ElementCrusher(TRSType.Scale, false)
			{
				hideFieldName = true,
				uniformAxes = ElementCrusher.UniformAxes.NonUniform,
				XCrusher = new FloatCrusher(BitPresets.Bits8, -1f, 1f, Axis.X, TRSType.Scale, true)
				{
					TRSType = TRSType.Scale,
					AccurateCenter = true,
					BitsDeterminedBy = BitsDeterminedBy.SetBits
				},
				YCrusher = new FloatCrusher(BitPresets.Bits8, -1f, 1f, Axis.Y, TRSType.Scale, true)
				{
					TRSType = TRSType.Scale,
					AccurateCenter = true,
					BitsDeterminedBy = BitsDeterminedBy.SetBits,
					Enabled = false
				},
				ZCrusher = new FloatCrusher(BitPresets.Bits8, -1f, 1f, Axis.Z, TRSType.Scale, true)
				{
					TRSType = TRSType.Scale,
					AccurateCenter = true,
					BitsDeterminedBy = BitsDeterminedBy.SetBits,
					Enabled = false
				}
			}
		};

		protected bool hasTeleported;

		protected int teleNewParentId;

		protected Matrix preTeleportM = new Matrix();

		protected CompressedMatrix preTeleportCM = new CompressedMatrix();

		private Rigidbody rb;

		private Rigidbody2D rb2d;

		private List<ITransformController> iTransformControllers = new List<ITransformController>(1);

		protected bool allowInterpolation;

		protected bool allowReconstructionOfEmpty;

		public static Stack<Frame[]> framePool = new Stack<Frame[]>();

		public Frame prevSentFrame;

		protected bool skipInterpolation;

		public override bool AllowInterpolation
		{
			get
			{
				return allowInterpolation;
			}
		}

		public override bool AllowReconstructionOfEmpty
		{
			get
			{
				return allowReconstructionOfEmpty;
			}
		}

		public override int ApplyOrder
		{
			get
			{
				return 9;
			}
		}

		public void FlagTeleport()
		{
			if (base.IsMine)
			{
				if (!hasTeleported)
				{
					CaptureCurrent(preTeleportM, preTeleportCM);
				}
				hasTeleported = true;
			}
		}

		public void UpdateParent(ObjState state, Transform newParent)
		{
			teleNewParentId = (newParent ? newParent.GetInstanceID() : (((state & ObjState.Mounted) != 0) ? (-2) : (-1)));
		}

		public override void OnAwake()
		{
			base.OnAwake();
			rb = GetComponent<Rigidbody>();
			rb2d = GetComponent<Rigidbody2D>();
			GetComponents(iTransformControllers);
			teleportThresholdSqrMag = ((teleportThreshold <= 0f) ? 0f : (teleportThreshold * teleportThreshold));
			ConnectSharedCaches();
			allowInterpolation = true;
			allowReconstructionOfEmpty = true;
			for (int i = 0; i < iTransformControllers.Count; i++)
			{
				ITransformController transformController = iTransformControllers[i];
				allowInterpolation &= !transformController.HandlesInterpolation;
				allowReconstructionOfEmpty &= !transformController.HandlesExtrapolation;
			}
		}

		private void ConnectSharedCaches()
		{
			if (masterSharedCrushers.ContainsKey(prefabInstanceId))
			{
				transformCrusher = masterSharedCrushers[prefabInstanceId];
			}
			else
			{
				masterSharedCrushers.Add(prefabInstanceId, transformCrusher);
			}
		}

		private void OnDestroy()
		{
			framePool.Push(frames);
		}

		protected override void PopulateFrames()
		{
			int frameCount = TickEngineSettings.frameCount;
			if (framePool.Count == 0)
			{
				frames = new Frame[frameCount + 1];
				frames[frameCount] = new Frame(this, frameCount);
				for (int i = 0; i <= frameCount; i++)
				{
					frames[i] = new Frame(frames[frameCount], i);
				}
			}
			else
			{
				frames = framePool.Pop();
				frames[frameCount].Set(this, frameCount);
				for (int j = 0; j < frameCount; j++)
				{
					frames[j].CopyFrom(frames[frameCount]);
				}
			}
		}

		protected void CaptureCurrent(Matrix m, CompressedMatrix cm, bool forceUseTransform = false)
		{
			if (forceUseTransform)
			{
				transformCrusher.Capture(base.transform, cm, m);
			}
			else if ((bool)rb)
			{
				transformCrusher.Capture(rb, cm, m);
			}
			else if ((bool)rb2d)
			{
				transformCrusher.Capture(rb2d, cm, m);
			}
			else
			{
				transformCrusher.Capture(base.transform, cm, m);
			}
		}

		public virtual void OnCaptureCurrentState(int frameId)
		{
			Frame frame = frames[frameId];
			frame.hasTeleported = hasTeleported;
			if (hasTeleported)
			{
				frame.cm.CopyFrom(preTeleportCM);
				frame.m.CopyFrom(preTeleportM);
				CaptureCurrent(frame.telem, frame.telecm, true);
				transformCrusher.Apply(base.transform, frame.telem);
				hasTeleported = false;
			}
			else
			{
				CaptureCurrent(frame.m, frame.cm);
			}
		}

		public SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{
			Frame frame = frames[frameId];
			bool flag = frame.hasTeleported;
			bool flag2 = flag || (writeFlags & SerializationFlags.NewConnection) != 0;
			if (!flag2 && !base.isActiveAndEnabled)
			{
				buffer.WriteBool(false, ref bitposition);
				return SerializationFlags.None;
			}
			bool flag3 = IsKeyframe(frameId);
			if (!flag2 && !flag3 && (!useDeltas || (prevSentFrame != null && frame.cm.Equals(prevSentFrame.cm))))
			{
				buffer.WriteBool(false, ref bitposition);
				prevSentFrame = frame;
				return SerializationFlags.None;
			}
			SerializationFlags serializationFlags = SerializationFlags.HasContent;
			buffer.WriteBool(true, ref bitposition);
			buffer.WriteBool(flag, ref bitposition);
			if (flag)
			{
				transformCrusher.Write(frame.telecm, buffer, ref bitposition);
				if (teleportReliable)
				{
					serializationFlags |= SerializationFlags.ForceReliable;
				}
			}
			transformCrusher.Write(frame.cm, buffer, ref bitposition);
			transformCrusher.Decompress(frame.m, frame.cm);
			prevSentFrame = frame;
			return serializationFlags;
		}

		public SerializationFlags OnNetDeserialize(int originFrameId, byte[] buffer, ref int bitposition, FrameArrival arrival)
		{
			Frame frame = (photonView.IsMine ? offtickFrame : frames[originFrameId]);
			if (!buffer.ReadBool(ref bitposition))
			{
				frame.content = FrameContents.Empty;
				return SerializationFlags.None;
			}
			frame.content = FrameContents.Complete;
			if (frame.hasTeleported = buffer.ReadBool(ref bitposition))
			{
				transformCrusher.Read(frame.telecm, buffer, ref bitposition);
				transformCrusher.Decompress(frame.telem, frame.telecm);
			}
			transformCrusher.Read(frame.cm, buffer, ref bitposition);
			transformCrusher.Decompress(frame.m, frame.cm);
			return SerializationFlags.HasContent;
		}

		public override bool OnSnapshot(int prevFrameId, int snapFrameId, int targFrameId, bool prevIsValid, bool snapIsValid, bool targIsValid)
		{
			if (!base.OnSnapshot(prevFrameId, snapFrameId, targFrameId, prevIsValid, snapIsValid, targIsValid))
			{
				return false;
			}
			if (snapFrame.content == FrameContents.Empty)
			{
				return false;
			}
			if (targFrame.content == FrameContents.Empty)
			{
				return false;
			}
			bool flag = snapFrame.hasTeleported;
			targFrame.parentHash = teleNewParentId;
			skipInterpolation = false;
			if (!flag && teleportThresholdSqrMag > 0f)
			{
				Vector3 position = targFrame.m.position;
				Vector3 vector = (flag ? snapFrame.telem.position : snapFrame.m.position);
				if (Vector3.SqrMagnitude(position - vector) > teleportThresholdSqrMag)
				{
					skipInterpolation = true;
				}
			}
			ApplyFrame(snapFrame);
			return true;
		}

		protected void ApplyFrame(Frame frame)
		{
			transformCrusher.Apply(base.transform, frame.hasTeleported ? frame.telem : frame.m);
		}

		public override bool OnInterpolate(int snapFrameId, int targFrameId, float t)
		{
			if (skipInterpolation)
			{
				return false;
			}
			if (!base.OnInterpolate(snapFrameId, targFrameId, t))
			{
				return false;
			}
			if (interpolation == Interpolation.None)
			{
				return false;
			}
			if (targFrame == null)
			{
				return false;
			}
			if (snapFrame.content == FrameContents.Empty)
			{
				return false;
			}
			if (targFrame.content == FrameContents.Empty)
			{
				return false;
			}
			if (snapFrame.parentHash != targFrame.parentHash)
			{
				return false;
			}
			Matrix start = (snapFrame.hasTeleported ? snapFrame.telem : snapFrame.m);
			if (interpolation == Interpolation.Linear)
			{
				Matrix.Lerp(Matrix.reusable, start, targFrame.m, t);
			}
			else
			{
				Matrix.CatmullRomLerpUnclamped(Matrix.reusable, prevFrame.m, snapFrame.m, targFrame.m, t);
			}
			transformCrusher.Apply(base.transform, Matrix.reusable);
			return true;
		}

		protected override void InterpolateFrame(Frame targframe, Frame startframe, Frame endframe, float t)
		{
			if (startframe.parentHash == -2 || startframe.parentHash != endframe.parentHash)
			{
				targFrame.content = FrameContents.Empty;
				return;
			}
			targframe.CopyFrom(endframe);
			Matrix.Lerp(targframe.m, startframe.hasTeleported ? startframe.telem : startframe.m, endframe.m, t);
			transformCrusher.Compress(targframe.cm, targframe.m);
		}

		protected override void ExtrapolateFrame(Frame prevframe, Frame snapframe, Frame targframe)
		{
			if (snapframe.content == FrameContents.Empty)
			{
				Debug.LogError(targframe.frameId + " Failed to extrapolate due to empty snapshot. Failsafing to current transform value.");
				targframe.content = FrameContents.Empty;
				return;
			}
			targframe.CopyFrom(snapframe);
			targframe.parentHash = teleNewParentId;
			if (snapframe.hasTeleported)
			{
				return;
			}
			switch (prevframe.content)
			{
			case FrameContents.Empty:
				return;
			case FrameContents.Complete:
			{
				int parentHash = prevframe.parentHash;
				if (parentHash == -2 || prevframe.hasTeleported || parentHash != snapframe.parentHash)
				{
					return;
				}
				break;
			}
			}
			Matrix.LerpUnclamped(targframe.m, prevframe.hasTeleported ? prevframe.telem : prevframe.m, snapframe.m, 1f + extrapolateRatio);
			transformCrusher.Compress(targframe.cm, targframe.m);
		}
	}
}
