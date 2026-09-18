using System;
using System.Collections.Generic;
using Photon.Compression;
using UnityEngine;

namespace Photon.Pun.Simple
{
	public class SyncNodeMover : SyncMoverBase<SyncNodeMover.TRSDefinition, SyncNodeMover.Frame>, IOnPreUpdate, IOnPreSimulate, IOnCaptureState, IOnNetSerialize, IOnSnapshot, IOnInterpolate, IReadyable
	{
		public enum Movement
		{
			Oscillate = 0,
			Trigger = 1
		}

		[Serializable]
		public class Node
		{
			public Vector3[] trs = new Vector3[3]
			{
				new Vector3(0f, 0f, 0f),
				new Vector3(0f, 0f, 0f),
				new Vector3(1f, 1f, 1f)
			};

			public Vector3 Pos
			{
				get
				{
					return trs[0];
				}
				set
				{
					trs[0] = value;
				}
			}

			public Vector3 Rot
			{
				get
				{
					return trs[1];
				}
				set
				{
					trs[1] = value;
				}
			}

			public Vector3 Scl
			{
				get
				{
					return trs[2];
				}
				set
				{
					trs[2] = value;
				}
			}
		}

		[Serializable]
		public class TRSDefinition : TRSDefinitionBase
		{
			public AxisMask includeAxes = AxisMask.XYZ;

			public MovementRelation relation = MovementRelation.Relative;
		}

		public class Frame : FrameBase
		{
			public int targetNode;

			public float phase;

			public uint cphase;

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
				targetNode = frame.targetNode;
				phase = frame.phase;
				cphase = frame.cphase;
			}

			public override void Clear()
			{
				base.Clear();
				targetNode = -1;
				phase = -1f;
				cphase = 0u;
			}

			public bool Compare(Frame otherFrame)
			{
				if (targetNode != otherFrame.targetNode || phase != otherFrame.phase || cphase != otherFrame.cphase)
				{
					return false;
				}
				return true;
			}
		}

		[Range(0f, 2f)]
		public float predictWithRTT = 1f;

		[HideInInspector]
		public List<Node> nodes = new List<Node>
		{
			new Node(),
			new Node()
		};

		[HideInInspector]
		public Movement movement;

		[HideInInspector]
		public float oscillatePeriod = 1f;

		[HideInInspector]
		public AnimationCurve oscillateCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

		[HideInInspector]
		public LiteFloatCrusher floatCrusher = new LiteFloatCrusher(LiteFloatCompressType.Bits10, LiteFloatCrusher.Normalization.Positive);

		protected float currentPhase;

		protected int queuedTargetNode;

		protected int targetNode;

		protected double timeoffset;

		protected float snapPhase;

		protected float targPhase;

		protected float accumulatedTime;

		public Node StartNode
		{
			get
			{
				return nodes[0];
			}
		}

		public Node EndNode
		{
			get
			{
				return nodes[nodes.Count - 1];
			}
		}

		protected override void Reset()
		{
			base.Reset();
			oscillateCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));
		}

		protected override void InitializeTRS(TRSDefinition def, TRS type)
		{
			if (movement != 0)
			{
				def.relation = MovementRelation.Relative;
			}
			if ((def.relation == MovementRelation.Relative && movement == Movement.Oscillate) || movement == Movement.Trigger)
			{
				Vector3 vector;
				switch (type)
				{
				case TRS.Position:
					vector = (def.local ? base.transform.localPosition : base.transform.position);
					break;
				case TRS.Rotation:
					vector = (def.local ? base.transform.localEulerAngles : base.transform.eulerAngles);
					break;
				default:
					vector = (def.local ? base.transform.localScale : base.transform.lossyScale);
					break;
				}
				nodes[0].trs[(int)type] += vector;
				nodes[1].trs[(int)type] += vector;
			}
		}

		public override void OnPreSimulate(int frameId, int subFrameId)
		{
			if (base.isActiveAndEnabled && (!photonView || photonView.IsMine))
			{
				OwnerInterpolate();
			}
		}

		public override void OnPreUpdate()
		{
			if (base.isActiveAndEnabled && (!photonView || photonView.IsMine))
			{
				OwnerInterpolate();
			}
		}

		private void OwnerInterpolate()
		{
			if (timeoffset == 0.0)
			{
				timeoffset = DoubleTime.fixedTime;
			}
			if (movement == Movement.Oscillate)
			{
				currentPhase = TimeToPhase(DoubleTime.time - timeoffset);
				float lerpT = OscillatePhaseToLerpT(currentPhase);
				Oscillate(lerpT);
			}
		}

		public void Trigger(int targetNode)
		{
			queuedTargetNode = targetNode;
		}

		public void TriggerMin()
		{
		}

		public void TriggerMax()
		{
		}

		private void TriggerLerp()
		{
		}

		private void Oscillate(float lerpT)
		{
			Node node = nodes[0];
			Node node2 = nodes[1];
			Vector3 pos = ((posDef.includeAxes == AxisMask.None) ? new Vector3(0f, 0f, 0f) : Vector3.Lerp(node.trs[0], node2.trs[0], lerpT));
			Vector3 rot = ((rotDef.includeAxes == AxisMask.None) ? new Vector3(0f, 0f, 0f) : Vector3.Lerp(node.trs[1], node2.trs[1], lerpT));
			Vector3 scl = ((sclDef.includeAxes == AxisMask.None) ? new Vector3(1f, 1f, 1f) : Vector3.Lerp(node.trs[2], node2.trs[2], lerpT));
			ApplyOscillate(pos, rot, scl);
		}

		public void OnCaptureCurrentState(int frameId)
		{
			Frame obj = frames[frameId];
			obj.targetNode = targetNode;
			obj.phase = currentPhase;
			obj.cphase = (uint)floatCrusher.Encode(currentPhase);
		}

		public SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{
			Frame frame = frames[frameId];
			if (movement == Movement.Oscillate)
			{
				floatCrusher.WriteValue(frame.phase, buffer, ref bitposition);
			}
			return SerializationFlags.HasContent;
		}

		public SerializationFlags OnNetDeserialize(int originFrameId, byte[] buffer, ref int bitposition, FrameArrival arrival)
		{
			Frame frame = frames[originFrameId];
			frame.content = FrameContents.Complete;
			if (movement == Movement.Oscillate)
			{
				frame.cphase = (uint)floatCrusher.ReadCValue(buffer, ref bitposition);
				frame.phase = floatCrusher.Decode(frame.cphase);
			}
			return SerializationFlags.HasContent;
		}

		protected override void ApplySnapshot(Frame snapframe, Frame targframe, bool snapIsVaid, bool targIsValid)
		{
			if (snapIsVaid && snapframe.content == FrameContents.Complete && movement == Movement.Oscillate)
			{
				if (predictWithRTT != 0f)
				{
					float num = (NetMaster.RTT + TickEngineSettings.targetBufferInterval) * predictWithRTT;
					float num2 = snapframe.phase * oscillatePeriod + num;
					float num3 = targframe.phase * oscillatePeriod + num;
					snapPhase = TimeToPhase(num2);
					targPhase = TimeToPhase(num3);
				}
				else
				{
					snapPhase = snapframe.phase;
					targPhase = targframe.phase;
				}
				float lerpT = OscillatePhaseToLerpT(snapPhase);
				Oscillate(lerpT);
			}
		}

		public override bool OnInterpolate(int snapFrameId, int targFrameId, float t)
		{
			if (!base.OnInterpolate(snapFrameId, targFrameId, t))
			{
				return false;
			}
			switch (movement)
			{
			case Movement.Oscillate:
			{
				if (targPhase < snapPhase)
				{
					targPhase += 1f;
				}
				float num = Mathf.Lerp(snapPhase, targPhase, t);
				if (num >= 1f)
				{
					num -= 1f;
				}
				float lerpT = OscillatePhaseToLerpT(num);
				Oscillate(lerpT);
				return true;
			}
			default:
				return false;
			}
		}

		protected override void ConstructMissingFrame(Frame prevFrame, Frame snapframe, Frame targframe)
		{
			if (movement == Movement.Oscillate && snapframe.content == FrameContents.Complete)
			{
				float num = snapframe.phase * oscillatePeriod + TickEngineSettings.netTickInterval;
				targframe.phase = TimeToPhase(num);
				targframe.content = FrameContents.Complete;
			}
		}

		protected float OscillatePhaseToLerpT(float phase)
		{
			return oscillateCurve.Evaluate(phase);
		}

		protected float TimeToPhase(double time)
		{
			return (float)(time % (double)oscillatePeriod) / oscillatePeriod;
		}

		private void ApplyOscillate(Vector3 pos, Vector3 rot, Vector3 scl)
		{
			AxisMask includeAxes = posDef.includeAxes;
			AxisMask includeAxes2 = rotDef.includeAxes;
			AxisMask includeAxes3 = sclDef.includeAxes;
			if (includeAxes3 != 0)
			{
				base.transform.localScale = new Vector3(((includeAxes3 & AxisMask.X) != 0) ? scl.x : base.transform.localScale.x, ((includeAxes3 & AxisMask.Y) != 0) ? scl.y : base.transform.localScale.y, ((includeAxes3 & AxisMask.Z) != 0) ? scl.z : base.transform.localScale.z);
			}
			if (includeAxes2 != 0)
			{
				if (rotDef.local)
				{
					base.transform.localEulerAngles = new Vector3(((includeAxes2 & AxisMask.X) != 0) ? rot.x : base.transform.localEulerAngles.x, ((includeAxes2 & AxisMask.Y) != 0) ? rot.y : base.transform.localEulerAngles.y, ((includeAxes2 & AxisMask.Z) != 0) ? rot.z : base.transform.localEulerAngles.z);
				}
				else
				{
					base.transform.eulerAngles = new Vector3(((includeAxes2 & AxisMask.X) != 0) ? rot.x : base.transform.eulerAngles.x, ((includeAxes2 & AxisMask.Y) != 0) ? rot.y : base.transform.eulerAngles.y, ((includeAxes2 & AxisMask.Z) != 0) ? rot.z : base.transform.eulerAngles.z);
				}
			}
			if (includeAxes != 0)
			{
				if (posDef.local)
				{
					base.transform.localPosition = new Vector3(((includeAxes & AxisMask.X) != 0) ? pos.x : base.transform.localPosition.x, ((includeAxes & AxisMask.Y) != 0) ? pos.y : base.transform.localPosition.y, ((includeAxes & AxisMask.Z) != 0) ? pos.z : base.transform.localPosition.z);
				}
				else
				{
					base.transform.position = new Vector3(((includeAxes & AxisMask.X) != 0) ? pos.x : base.transform.position.x, ((includeAxes & AxisMask.Y) != 0) ? pos.y : base.transform.position.y, ((includeAxes & AxisMask.Z) != 0) ? pos.z : base.transform.position.z);
				}
			}
		}
	}
}
