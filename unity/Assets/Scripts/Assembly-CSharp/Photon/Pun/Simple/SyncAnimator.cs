using System;
using System.Collections.Generic;
using Photon.Compression;
using Photon.Pun.Simple.Internal;
using Photon.Utilities;
using UnityEngine;
using emotitron.Compression;

namespace Photon.Pun.Simple
{
	public class SyncAnimator : SyncObject<SyncAnimator.Frame>, IOnSnapshot, IOnNetSerialize, IOnAuthorityChanged, ISyncAnimator, IReadyable, IUseKeyframes, IOnInterpolate, IOnCaptureState
	{
		public class Frame : FrameBase
		{
			public SyncAnimator syncAnimator;

			public SmartVar[] parameters;

			public int?[] stateHashes;

			public bool[] layerIsInTransition;

			public float[] normalizedTime;

			public float?[] layerWeights;

			public Queue<AnimPassThru> passThrus;

			public Frame()
			{
			}

			public Frame(SyncAnimator syncAnimator, int frameId)
				: base(frameId)
			{
				this.syncAnimator = syncAnimator;
				int layerCount = syncAnimator.layerCount;
				stateHashes = new int?[layerCount];
				layerIsInTransition = new bool[layerCount];
				normalizedTime = new float[layerCount];
				layerWeights = new float?[layerCount];
				passThrus = new Queue<AnimPassThru>(2);
				parameters = new SmartVar[syncAnimator.paramCount];
				int paramCount = syncAnimator.paramCount;
				for (int i = 0; i < paramCount; i++)
				{
					parameters[i] = syncAnimator.sharedParamSettings[i].defaultValue;
				}
			}

			public override void CopyFrom(FrameBase sourceFrame)
			{
				base.CopyFrom(sourceFrame);
				Frame frame = sourceFrame as Frame;
				if (syncAnimator.syncParams)
				{
					SmartVar[] array = frame.parameters;
					int num = array.Length;
					for (int i = 0; i < num; i++)
					{
						parameters[i] = array[i];
					}
				}
				if (syncAnimator.syncStates)
				{
					int num2 = frame.stateHashes.Length;
					for (int j = 0; j < num2; j++)
					{
						stateHashes[j] = frame.stateHashes[j];
						layerIsInTransition[j] = frame.layerIsInTransition[j];
						normalizedTime[j] = frame.normalizedTime[j];
						layerWeights[j] = frame.layerWeights[j];
					}
				}
			}

			public override void Clear()
			{
				base.Clear();
				int i = 0;
				for (int num = stateHashes.Length; i < num; i++)
				{
					stateHashes[i] = null;
				}
				passThrus.Clear();
				int j = 0;
				for (int num2 = layerWeights.Length; j < num2; j++)
				{
					layerWeights[j] = null;
					stateHashes[j] = null;
					normalizedTime[j] = 0f;
					layerWeights[j] = null;
				}
			}
		}

		private static Dictionary<int, Dictionary<int, int>> masterSharedTriggHashes = new Dictionary<int, Dictionary<int, int>>();

		private static Dictionary<int, List<int>> masterSharedTriggIndexes = new Dictionary<int, List<int>>();

		[HideInInspector]
		public List<int> sharedTriggIndexes = new List<int>();

		private Dictionary<int, int> sharedTriggHashes;

		private static Dictionary<int, Dictionary<int, int>> masterSharedStateHashes = new Dictionary<int, Dictionary<int, int>>();

		private static Dictionary<int, List<int>> masterSharedStateIndexes = new Dictionary<int, List<int>>();

		[HideInInspector]
		public List<int> sharedStateIndexes = new List<int>();

		private Dictionary<int, int> sharedStateHashes;

		[Tooltip("The Animator to sync. If null the first animator on this game object will be found and used.")]
		public Animator animator;

		[Tooltip("Disables applyRootMotion on any non-authority instances, to avoid a tug of war between the transform sync and root motion.")]
		public bool autoRootMotion = true;

		[HideInInspector]
		public bool syncPassThrus = true;

		[HideInInspector]
		public NormalizedFloatCompression passthruNormTimeCompress = NormalizedFloatCompression.Bits10;

		[HideInInspector]
		public bool syncStates = true;

		[HideInInspector]
		public NormalizedFloatCompression normalizedTimeCompress = NormalizedFloatCompression.Bits10;

		[HideInInspector]
		public bool syncLayers = true;

		[HideInInspector]
		public bool syncLayerWeights = true;

		[HideInInspector]
		public NormalizedFloatCompression layerWeightCompress = NormalizedFloatCompression.Bits10;

		[NonSerialized]
		public int layerCount;

		[HideInInspector]
		public bool syncParams = true;

		[HideInInspector]
		public bool useGlobalParamSettings = true;

		private static Dictionary<int, ParameterDefaults> masterSharedParamDefaults = new Dictionary<int, ParameterDefaults>();

		[HideInInspector]
		public ParameterDefaults sharedParamDefaults = new ParameterDefaults();

		private static Dictionary<int, ParameterSettings[]> masterSharedParamSettings = new Dictionary<int, ParameterSettings[]>();

		[HideInInspector]
		public ParameterSettings[] sharedParamSettings = new ParameterSettings[0];

		[HideInInspector]
		public int paramCount;

		private int bitsForTriggerIndex;

		private int bitsForStateIndex;

		private int bitsForTransIndex;

		private int bitsForLayerIndex;

		private bool defaultRootMotion;

		private int[] lastAnimationHash;

		private uint[] lastLayerWeight;

		private SmartVar[] lastSentParams;

		private Frame currentFrame;

		public static Dictionary<int, Stack<Frame[]>> masterSharedFramePool = new Dictionary<int, Stack<Frame[]>>();

		private static List<Animator> foundAnimators = new List<Animator>();

		private static List<SyncAnimator> foundSyncs = new List<SyncAnimator>();

		private readonly Queue<AnimPassThru> passThruQueue = new Queue<AnimPassThru>(2);

		public override int ApplyOrder
		{
			get
			{
				return 11;
			}
		}

		public override bool AllowReconstructionOfEmpty
		{
			get
			{
				return false;
			}
		}

		protected override void PopulateFrames()
		{
			Initialize();
			int frameCount = TickEngineSettings.frameCount;
			Stack<Frame[]> value;
			if (!masterSharedFramePool.TryGetValue(prefabInstanceId, out value))
			{
				value = new Stack<Frame[]>();
				masterSharedFramePool.Add(prefabInstanceId, value);
			}
			if (value.Count == 0)
			{
				frames = new Frame[frameCount + 1];
				for (int i = 0; i <= frameCount; i++)
				{
					frames[i] = new Frame(this, i);
				}
			}
			else
			{
				frames = value.Pop();
				for (int j = 0; j <= frameCount; j++)
				{
					frames[j].Clear();
				}
			}
		}

		public override void OnAwake()
		{
			if (animator == null)
			{
				FindUnsyncedAnimator();
			}
			base.OnAwake();
			ConnectSharedCaches();
			if ((bool)animator)
			{
				defaultRootMotion = animator.applyRootMotion;
			}
		}

		public override void OnJoinedRoom()
		{
			base.OnJoinedRoom();
			AutoRootMotion(base.IsMine);
		}

		public override void OnStart()
		{
			base.OnStart();
			AutoRootMotion(base.IsMine);
		}

		public override void OnAuthorityChanged(bool isMine, bool controllerChanged)
		{
			base.OnAuthorityChanged(isMine, controllerChanged);
			AutoRootMotion(isMine);
		}

		private void AutoRootMotion(bool isMine)
		{
			if (autoRootMotion && (bool)animator)
			{
				animator.applyRootMotion = isMine && defaultRootMotion;
			}
		}

		private void FindUnsyncedAnimator()
		{
			base.transform.GetNestedComponentsInChildren<Animator, NetObject>(foundAnimators);
			base.transform.GetNestedComponentsInChildren<SyncAnimator, NetObject>(foundSyncs);
			foreach (Animator foundAnimator in foundAnimators)
			{
				bool flag = false;
				foreach (SyncAnimator foundSync in foundSyncs)
				{
					flag = false;
					if (foundSync.animator == foundAnimator)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					animator = foundAnimator;
					break;
				}
			}
		}

		private void Initialize()
		{
			bitsForTriggerIndex = (sharedTriggIndexes.Count - 1).GetBitsForMaxValue();
			bitsForStateIndex = (sharedStateIndexes.Count - 1).GetBitsForMaxValue();
			paramCount = animator.parameters.Length;
			layerCount = animator.layerCount;
			bitsForLayerIndex = layerCount.GetBitsForMaxValue();
			lastSentParams = new SmartVar[paramCount];
			ParameterSettings.RebuildParamSettings(animator, ref sharedParamSettings, ref paramCount, sharedParamDefaults);
			lastAnimationHash = new int[layerCount];
			lastLayerWeight = new uint[layerCount];
			for (int i = 0; i < paramCount; i++)
			{
				lastSentParams[i] = sharedParamSettings[i].defaultValue;
			}
		}

		private void ConnectSharedCaches()
		{
			if (!masterSharedTriggHashes.ContainsKey(prefabInstanceId))
			{
				sharedTriggHashes = new Dictionary<int, int>();
				for (int i = 0; i < sharedTriggIndexes.Count; i++)
				{
					if (sharedTriggHashes.ContainsKey(sharedTriggIndexes[i]))
					{
						Debug.LogError("There appear to be duplicate Trigger names in the animator controller on '" + base.name + "'. This will break " + GetType().Name + "'s ability to sync triggers.");
					}
					else
					{
						sharedTriggHashes.Add(sharedTriggIndexes[i], i);
					}
				}
				masterSharedTriggHashes.Add(prefabInstanceId, sharedTriggHashes);
				masterSharedTriggIndexes.Add(prefabInstanceId, sharedTriggIndexes);
			}
			else
			{
				sharedTriggHashes = masterSharedTriggHashes[prefabInstanceId];
				sharedTriggIndexes = masterSharedTriggIndexes[prefabInstanceId];
			}
			if (!masterSharedStateHashes.ContainsKey(prefabInstanceId))
			{
				sharedStateHashes = new Dictionary<int, int>();
				for (int j = 0; j < sharedStateIndexes.Count; j++)
				{
					if (sharedStateHashes.ContainsKey(sharedStateIndexes[j]))
					{
						Debug.LogError("There appear to be duplicate State names in the animator controller on '" + base.name + "'. This will break " + GetType().Name + "'s ability to sync states.");
					}
					else
					{
						sharedStateHashes.Add(sharedStateIndexes[j], j);
					}
				}
				masterSharedStateHashes.Add(prefabInstanceId, sharedStateHashes);
				masterSharedStateIndexes.Add(prefabInstanceId, sharedStateIndexes);
			}
			else
			{
				sharedStateHashes = masterSharedStateHashes[prefabInstanceId];
				sharedStateIndexes = masterSharedStateIndexes[prefabInstanceId];
			}
			ParameterDefaults value;
			if (masterSharedParamDefaults.TryGetValue(prefabInstanceId, out value))
			{
				sharedParamDefaults = value;
			}
			else
			{
				masterSharedParamDefaults.Add(prefabInstanceId, sharedParamDefaults);
			}
			ParameterSettings[] value2;
			if (masterSharedParamSettings.TryGetValue(prefabInstanceId, out value2))
			{
				sharedParamSettings = value2;
			}
			else
			{
				masterSharedParamSettings.Add(prefabInstanceId, sharedParamSettings);
			}
		}

		private void OnDestroy()
		{
			masterSharedFramePool[prefabInstanceId].Push(frames);
		}

		public SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
		{
			Frame frame = frames[frameId];
			if (frame.content == FrameContents.Empty)
			{
				buffer.WriteBool(false, ref bitposition);
				return SerializationFlags.None;
			}
			buffer.WriteBool(true, ref bitposition);
			bool isKeyframe = IsKeyframe(frameId);
			return WriteAllToBuffer(frame, buffer, ref bitposition, isKeyframe);
		}

		public SerializationFlags OnNetDeserialize(int originFrameId, byte[] buffer, ref int bitposition, FrameArrival arrival)
		{
			bool isKeyframe = IsKeyframe(originFrameId);
			Frame frame = (base.IsMine ? offtickFrame : frames[originFrameId]);
			if (!buffer.ReadBool(ref bitposition))
			{
				return SerializationFlags.None;
			}
			frame.content = FrameContents.Complete;
			ReadAllFromBuffer(frame, buffer, ref bitposition, isKeyframe);
			return SerializationFlags.HasContent;
		}

		public virtual void OnCaptureCurrentState(int frameId)
		{
			Frame frame = frames[frameId];
			if (!base.isActiveAndEnabled || !animator.isActiveAndEnabled)
			{
				frame.content = FrameContents.Empty;
				return;
			}
			if (syncParams)
			{
				CaptureParameters(frame);
			}
			if (syncPassThrus)
			{
				CapturePassThrus(frame);
			}
			if (syncStates)
			{
				CaptureStates(frame);
			}
			if (syncStates)
			{
				CaptureLayerWeights(frame);
			}
			frame.content = FrameContents.Complete;
		}

		private SerializationFlags WriteAllToBuffer(Frame frame, byte[] buffer, ref int bitposition, bool isKeyframe)
		{
			SerializationFlags serializationFlags = SerializationFlags.None;
			if (syncPassThrus)
			{
				serializationFlags |= WritePassThrus(frame, buffer, ref bitposition, isKeyframe);
			}
			if (syncParams)
			{
				serializationFlags |= WriteParameters(frame, buffer, ref bitposition, isKeyframe);
			}
			if (syncStates)
			{
				serializationFlags |= WriteStates(frame, buffer, ref bitposition, isKeyframe);
			}
			if (syncLayerWeights)
			{
				serializationFlags |= WriteLayerWeights(frame, buffer, ref bitposition, isKeyframe);
			}
			return serializationFlags;
		}

		private void ReadAllFromBuffer(Frame frame, byte[] buffer, ref int bitposition, bool isKeyframe)
		{
			if (syncPassThrus)
			{
				ReadPassThrus(frame, buffer, ref bitposition, isKeyframe);
			}
			if (syncParams)
			{
				ReadParameters(frame, buffer, ref bitposition, isKeyframe);
			}
			if (syncStates)
			{
				ReadStates(frame, buffer, ref bitposition, isKeyframe);
			}
			if (syncLayerWeights)
			{
				ReadLayerWeights(frame, buffer, ref bitposition, isKeyframe);
			}
		}

		private SerializationFlags WriteParameters(Frame frame, byte[] buffer, ref int bitposition, bool isKeyframe)
		{
			SmartVar[] parameters = frame.parameters;
			SerializationFlags serializationFlags = SerializationFlags.None;
			for (int i = 0; i < paramCount; i++)
			{
				ParameterSettings parameterSettings = sharedParamSettings[i];
				if (!useGlobalParamSettings && !parameterSettings.include)
				{
					continue;
				}
				switch (parameterSettings.paramType)
				{
				case AnimatorControllerParameterType.Int:
				{
					int num3 = parameters[i];
					if (isKeyframe || num3 != (int)lastSentParams[i])
					{
						if (!isKeyframe)
						{
							buffer.WriteBool(true, ref bitposition);
						}
						if (useGlobalParamSettings)
						{
							buffer.WriteSignedPackedBytes(num3, ref bitposition, 32);
						}
						else
						{
							parameterSettings.icrusher.WriteValue(num3, buffer, ref bitposition);
						}
						lastSentParams[i] = num3;
						serializationFlags |= SerializationFlags.HasContent;
					}
					else if (!isKeyframe)
					{
						buffer.WriteBool(false, ref bitposition);
					}
					break;
				}
				case AnimatorControllerParameterType.Float:
				{
					float num = parameters[i];
					LiteFloatCrusher fcrusher = parameterSettings.fcrusher;
					uint num2 = (uint)(useGlobalParamSettings ? HalfUtilities.Pack(num) : fcrusher.Encode(num));
					if (isKeyframe || num2 != lastSentParams[i].UInt)
					{
						if (!isKeyframe)
						{
							buffer.WriteBool(true, ref bitposition);
						}
						if (useGlobalParamSettings)
						{
							buffer.Write(num2, ref bitposition, 16);
						}
						else
						{
							fcrusher.WriteCValue(num2, buffer, ref bitposition);
						}
						lastSentParams[i] = num2;
						serializationFlags |= SerializationFlags.HasContent;
					}
					else if (!isKeyframe)
					{
						buffer.WriteBool(false, ref bitposition);
					}
					break;
				}
				case AnimatorControllerParameterType.Bool:
				{
					bool flag2 = parameters[i];
					buffer.WriteBool(flag2, ref bitposition);
					if (isKeyframe || flag2 != (bool)lastSentParams[i])
					{
						serializationFlags |= SerializationFlags.HasContent;
					}
					break;
				}
				case AnimatorControllerParameterType.Trigger:
				{
					bool flag = parameters[i];
					buffer.WriteBool(flag, ref bitposition);
					if (isKeyframe || flag != (bool)lastSentParams[i])
					{
						serializationFlags |= SerializationFlags.HasContent;
					}
					break;
				}
				}
			}
			return serializationFlags;
		}

		private void CaptureParameters(Frame frame)
		{
			SmartVar[] parameters = frame.parameters;
			for (int i = 0; i < paramCount; i++)
			{
				ParameterSettings parameterSettings = sharedParamSettings[i];
				if (useGlobalParamSettings || parameterSettings.include)
				{
					AnimatorControllerParameterType paramType = parameterSettings.paramType;
					int hash = parameterSettings.hash;
					switch (paramType)
					{
					case AnimatorControllerParameterType.Float:
						parameters[i] = animator.GetFloat(hash);
						break;
					case AnimatorControllerParameterType.Int:
						parameters[i] = animator.GetInteger(hash);
						break;
					case AnimatorControllerParameterType.Bool:
						parameters[i] = animator.GetBool(hash);
						break;
					case AnimatorControllerParameterType.Trigger:
						parameters[i] = animator.GetBool(hash);
						break;
					}
				}
			}
		}

		private void ReadParameters(Frame frame, byte[] buffer, ref int bitposition, bool isKeyframe)
		{
			SmartVar[] parameters = frame.parameters;
			bool flag = frame == targFrame || frame == snapFrame;
			for (int i = 0; i < paramCount; i++)
			{
				ParameterSettings parameterSettings = sharedParamSettings[i];
				if (!useGlobalParamSettings && !parameterSettings.include)
				{
					continue;
				}
				switch (parameterSettings.paramType)
				{
				case AnimatorControllerParameterType.Int:
					if (isKeyframe || buffer.ReadBool(ref bitposition))
					{
						int num = (useGlobalParamSettings ? buffer.ReadSignedPackedBytes(ref bitposition, 32) : parameterSettings.icrusher.ReadValue(buffer, ref bitposition));
						parameters[i] = num;
					}
					else if (!flag)
					{
						parameters[i] = SmartVar.None;
					}
					break;
				case AnimatorControllerParameterType.Float:
					if (isKeyframe || buffer.ReadBool(ref bitposition))
					{
						parameters[i] = (useGlobalParamSettings ? buffer.ReadHalf(ref bitposition) : parameterSettings.fcrusher.ReadValue(buffer, ref bitposition));
					}
					else if (!flag)
					{
						parameters[i] = SmartVar.None;
					}
					break;
				case AnimatorControllerParameterType.Bool:
				{
					bool flag3 = buffer.ReadBool(ref bitposition);
					parameters[i] = flag3;
					break;
				}
				case AnimatorControllerParameterType.Trigger:
				{
					bool flag2 = buffer.ReadBool(ref bitposition);
					parameters[i] = flag2;
					break;
				}
				}
			}
		}

		private void CompleteTargetParameters()
		{
			SmartVar[] array = ((snapFrame != null) ? snapFrame.parameters : targFrame.parameters);
			SmartVar[] parameters = targFrame.parameters;
			for (int i = 0; i < paramCount; i++)
			{
				SmartVar smartVar = array[i];
				SmartVar smartVar2 = parameters[i];
				ParameterSettings parameterSettings = sharedParamSettings[i];
				if (smartVar.TypeCode == SmartVarTypeCode.None)
				{
					smartVar = (array[i] = parameterSettings.defaultValue);
				}
				if (smartVar2.TypeCode == SmartVarTypeCode.None)
				{
					parameters[i] = smartVar;
				}
			}
		}

		private void InterpolateParams(float t)
		{
			SmartVar[] parameters = snapFrame.parameters;
			SmartVar[] parameters2 = targFrame.parameters;
			for (int i = 0; i < paramCount; i++)
			{
				ParameterSettings parameterSettings = sharedParamSettings[i];
				int hash = parameterSettings.hash;
				if (!useGlobalParamSettings && !parameterSettings.include)
				{
					continue;
				}
				AnimatorControllerParameterType paramType = parameterSettings.paramType;
				SmartVar smartVar = parameters[i];
				SmartVar smartVar2 = parameters2[i];
				if (smartVar.TypeCode == SmartVarTypeCode.None || smartVar2.TypeCode == SmartVarTypeCode.None)
				{
					continue;
				}
				switch (paramType)
				{
				case AnimatorControllerParameterType.Int:
				{
					if (!sharedParamDefaults.includeInts)
					{
						break;
					}
					if (t == 0f)
					{
						animator.SetInteger(hash, smartVar);
						break;
					}
					ParameterInterpolation parameterInterpolation2 = ((!useGlobalParamSettings) ? parameterSettings.interpolate : sharedParamDefaults.interpolateInts);
					if (parameterInterpolation2 != ParameterInterpolation.Hold)
					{
						int num;
						switch (parameterInterpolation2)
						{
						default:
							num = parameterSettings.defaultValue;
							break;
						case ParameterInterpolation.Lerp:
							num = (int)Mathf.Lerp(smartVar, smartVar2, t);
							break;
						case ParameterInterpolation.Advance:
							num = smartVar2;
							break;
						}
						int value = num;
						animator.SetInteger(hash, value);
					}
					break;
				}
				case AnimatorControllerParameterType.Float:
				{
					if (!sharedParamDefaults.includeFloats)
					{
						break;
					}
					if (t == 0f)
					{
						animator.SetFloat(hash, smartVar);
						break;
					}
					ParameterInterpolation parameterInterpolation = ((!useGlobalParamSettings) ? parameterSettings.interpolate : sharedParamDefaults.interpolateFloats);
					if (parameterInterpolation != ParameterInterpolation.Hold)
					{
						SmartVar smartVar3;
						switch (parameterInterpolation)
						{
						default:
							smartVar3 = parameterSettings.defaultValue;
							break;
						case ParameterInterpolation.Advance:
							smartVar3 = smartVar2;
							break;
						case ParameterInterpolation.Lerp:
							smartVar3 = Mathf.Lerp(smartVar, smartVar2, t);
							break;
						}
						SmartVar smartVar4 = smartVar3;
						animator.SetFloat(hash, smartVar4);
					}
					break;
				}
				case AnimatorControllerParameterType.Bool:
					if (t == 0f && sharedParamDefaults.includeBools)
					{
						animator.SetBool(hash, smartVar);
					}
					break;
				case AnimatorControllerParameterType.Trigger:
					if (t == 0f && sharedParamDefaults.includeTriggers && (bool)smartVar)
					{
						animator.SetTrigger(hash);
					}
					break;
				}
			}
		}

		private void ExtrapolateParams(Frame prev, Frame targ, Frame newtarg)
		{
			if (prev == null)
			{
				return;
			}
			SmartVar[] parameters = prev.parameters;
			SmartVar[] parameters2 = targ.parameters;
			for (int i = 0; i < paramCount; i++)
			{
				ParameterSettings parameterSettings = sharedParamSettings[i];
				AnimatorControllerParameterType paramType = parameterSettings.paramType;
				if (!useGlobalParamSettings && !parameterSettings.include)
				{
					continue;
				}
				switch (paramType)
				{
				case AnimatorControllerParameterType.Float:
				{
					ParameterExtrapolation parameterExtrapolation2 = (useGlobalParamSettings ? sharedParamDefaults.extrapolateFloats : parameterSettings.extrapolate);
					SmartVar[] parameters3 = newtarg.parameters;
					int num = i;
					SmartVar smartVar;
					switch (parameterExtrapolation2)
					{
					default:
						smartVar = parameterSettings.defaultValue;
						break;
					case ParameterExtrapolation.Lerp:
						smartVar = (float)parameters2[i] + ((float)parameters2[i] - (float)parameters[i]);
						break;
					case ParameterExtrapolation.Hold:
						smartVar = parameters2[i];
						break;
					}
					parameters3[num] = smartVar;
					break;
				}
				case AnimatorControllerParameterType.Int:
				{
					ParameterExtrapolation parameterExtrapolation4 = (useGlobalParamSettings ? sharedParamDefaults.extrapolateInts : parameterSettings.extrapolate);
					SmartVar[] parameters4 = newtarg.parameters;
					int num2 = i;
					SmartVar smartVar2;
					switch (parameterExtrapolation4)
					{
					default:
						smartVar2 = parameterSettings.defaultValue;
						break;
					case ParameterExtrapolation.Lerp:
						smartVar2 = (int)parameters2[i] + ((int)parameters2[i] - (int)parameters[i]);
						break;
					case ParameterExtrapolation.Hold:
						smartVar2 = parameters2[i];
						break;
					}
					parameters4[num2] = smartVar2;
					break;
				}
				case AnimatorControllerParameterType.Bool:
				{
					ParameterExtrapolation parameterExtrapolation3 = (useGlobalParamSettings ? sharedParamDefaults.extrapolateBools : parameterSettings.extrapolate);
					newtarg.parameters[i] = ((parameterExtrapolation3 == ParameterExtrapolation.Hold) ? parameters2[i] : parameterSettings.defaultValue);
					break;
				}
				default:
				{
					ParameterExtrapolation parameterExtrapolation = (useGlobalParamSettings ? sharedParamDefaults.extrapolateTriggers : parameterSettings.extrapolate);
					newtarg.parameters[i] = ((parameterExtrapolation == ParameterExtrapolation.Hold) ? parameters2[i] : parameterSettings.defaultValue);
					break;
				}
				}
			}
		}

		private void EnqueuePassthrough(PassThruType type, int hash, int layer, float ntime, float otime, float duration, LocalApplyTiming localApplyTiming)
		{
			AnimPassThru animPassThru = new AnimPassThru(type, hash, layer, ntime, otime, duration, localApplyTiming);
			passThruQueue.Enqueue(animPassThru);
			if (localApplyTiming == LocalApplyTiming.Immediately || !syncPassThrus)
			{
				ExecutePassThru(animPassThru);
			}
		}

		public void SetTrigger(string triggerName, LocalApplyTiming localApplyTiming = LocalApplyTiming.OnSend)
		{
			int hash = Animator.StringToHash(triggerName);
			EnqueuePassthrough(PassThruType.SetTrigger, hash, -1, -1f, -1f, -1f, localApplyTiming);
		}

		public void SetTrigger(int hash, LocalApplyTiming localApplyTiming = LocalApplyTiming.OnSend)
		{
			EnqueuePassthrough(PassThruType.SetTrigger, hash, -1, -1f, -1f, -1f, localApplyTiming);
		}

		public void ResetTrigger(string triggerName, LocalApplyTiming localApplyTiming = LocalApplyTiming.OnSend)
		{
			int hash = Animator.StringToHash(triggerName);
			EnqueuePassthrough(PassThruType.ResetTrigger, hash, -1, -1f, -1f, -1f, localApplyTiming);
		}

		public void ResetTrigger(int hash, LocalApplyTiming localApplyTiming = LocalApplyTiming.OnSend)
		{
			EnqueuePassthrough(PassThruType.ResetTrigger, hash, -1, -1f, -1f, -1f, localApplyTiming);
		}

		public void Play(string stateName, int layer = -1, float normalizedTime = 0f, LocalApplyTiming localApplyTiming = LocalApplyTiming.OnSend)
		{
			int hash = Animator.StringToHash(stateName);
			EnqueuePassthrough(PassThruType.Play, hash, layer, normalizedTime, -1f, -1f, localApplyTiming);
		}

		public void Play(int hash, int layer = -1, float normalizedTime = 0f, LocalApplyTiming localApplyTiming = LocalApplyTiming.OnSend)
		{
			EnqueuePassthrough(PassThruType.Play, hash, layer, normalizedTime, -1f, -1f, localApplyTiming);
		}

		public void PlayInFixedTime(string stateName, int layer = -1, float fixedTime = 0f, LocalApplyTiming localApplyTiming = LocalApplyTiming.OnSend)
		{
			int hash = Animator.StringToHash(stateName);
			EnqueuePassthrough(PassThruType.PlayFixed, hash, layer, -1f, fixedTime, -1f, localApplyTiming);
		}

		public void PlayInFixedTime(int hash, int layer = -1, float fixedTime = 0f, LocalApplyTiming localApplyTiming = LocalApplyTiming.OnSend)
		{
			EnqueuePassthrough(PassThruType.PlayFixed, hash, layer, -1f, fixedTime, -1f, localApplyTiming);
		}

		public void CrossFade(string stateName, float duration, int layer = -1, float normalizedTime = 0f, LocalApplyTiming localApplyTiming = LocalApplyTiming.OnSend)
		{
			int hash = Animator.StringToHash(stateName);
			EnqueuePassthrough(PassThruType.CrossFade, hash, layer, normalizedTime, -1f, duration, localApplyTiming);
		}

		public void CrossFade(int hash, float duration, int layer = -1, float normalizedTime = 0f, LocalApplyTiming localApplyTiming = LocalApplyTiming.OnSend)
		{
			EnqueuePassthrough(PassThruType.CrossFade, hash, layer, normalizedTime, -1f, duration, localApplyTiming);
		}

		public void CrossFadeInFixedTime(string stateName, float duration, int layer = -1, float fixedTime = 0f, LocalApplyTiming localApplyTiming = LocalApplyTiming.OnSend)
		{
			int hash = Animator.StringToHash(stateName);
			EnqueuePassthrough(PassThruType.CrossFadeFixed, hash, layer, -1f, fixedTime, duration, localApplyTiming);
		}

		public void CrossFadeInFixedTime(int hash, float duration, int layer = -1, float fixedTime = 0f, LocalApplyTiming localApplyTiming = LocalApplyTiming.OnSend)
		{
			EnqueuePassthrough(PassThruType.CrossFadeFixed, hash, layer, -1f, fixedTime, duration, localApplyTiming);
		}

		private void ExecutePassThruQueue(Frame frame)
		{
			Queue<AnimPassThru> passThrus = frame.passThrus;
			while (passThrus.Count > 0)
			{
				AnimPassThru pt = passThrus.Dequeue();
				ExecutePassThru(pt);
			}
		}

		private void ExecutePassThru(AnimPassThru pt)
		{
			int hash = pt.hash;
			switch (pt.passThruType)
			{
			case PassThruType.SetTrigger:
				animator.SetTrigger(pt.hash);
				break;
			case PassThruType.ResetTrigger:
				animator.ResetTrigger(pt.hash);
				break;
			case PassThruType.Play:
				animator.Play(hash, pt.layer, pt.normlTime);
				break;
			case PassThruType.PlayFixed:
				animator.PlayInFixedTime(hash, pt.layer, pt.fixedTime);
				break;
			case PassThruType.CrossFade:
				animator.CrossFade(hash, pt.duration, pt.layer, pt.normlTime);
				break;
			case PassThruType.CrossFadeFixed:
				animator.CrossFadeInFixedTime(hash, pt.duration, pt.layer, pt.fixedTime);
				break;
			}
		}

		private SerializationFlags WritePassThrus(Frame frame, byte[] buffer, ref int bitposition, bool isKeyframe)
		{
			Queue<AnimPassThru> passThrus = frame.passThrus;
			SerializationFlags result = ((passThrus.Count != 0) ? SerializationFlags.HasContent : SerializationFlags.None);
			while (passThrus.Count > 0)
			{
				AnimPassThru pt = passThrus.Dequeue();
				PassThruType passThruType = pt.passThruType;
				int hash = pt.hash;
				bool flag = passThruType == PassThruType.SetTrigger || passThruType == PassThruType.ResetTrigger;
				if (pt.localApplyTiming == LocalApplyTiming.OnSend)
				{
					ExecutePassThru(pt);
				}
				buffer.WriteBool(true, ref bitposition);
				buffer.Write((uint)passThruType, ref bitposition, 3);
				int value;
				bool flag2 = (flag ? sharedTriggHashes.TryGetValue(hash, out value) : sharedStateHashes.TryGetValue(pt.hash, out value));
				buffer.WriteBool(flag2, ref bitposition);
				if (flag2)
				{
					buffer.Write((uint)value, ref bitposition, flag ? bitsForTriggerIndex : bitsForStateIndex);
				}
				else
				{
					buffer.WriteSigned(pt.hash, ref bitposition, 32);
				}
				if (flag)
				{
					continue;
				}
				if (layerCount > 1)
				{
					buffer.Write((uint)(pt.layer + 1), ref bitposition, bitsForLayerIndex);
				}
				if (passThruType == PassThruType.Play || passThruType == PassThruType.CrossFade)
				{
					float normlTime = pt.normlTime;
					if (normlTime == 0f)
					{
						buffer.WriteBool(false, ref bitposition);
					}
					else
					{
						buffer.WriteBool(true, ref bitposition);
						buffer.WriteNorm(normlTime, ref bitposition, (int)passthruNormTimeCompress);
					}
				}
				else
				{
					float fixedTime = pt.fixedTime;
					if (fixedTime == 0f)
					{
						buffer.WriteBool(false, ref bitposition);
					}
					else
					{
						buffer.WriteBool(true, ref bitposition);
						buffer.WriteHalf(fixedTime, ref bitposition);
					}
				}
				if (passThruType == PassThruType.CrossFade || passThruType == PassThruType.CrossFadeFixed)
				{
					buffer.WriteHalf(pt.duration, ref bitposition);
				}
			}
			buffer.WriteBool(false, ref bitposition);
			return result;
		}

		private void CapturePassThrus(Frame frame)
		{
			if (syncPassThrus)
			{
				while (passThruQueue.Count > 0)
				{
					frame.passThrus.Enqueue(passThruQueue.Dequeue());
				}
			}
		}

		private void ReadPassThrus(Frame frame, byte[] buffer, ref int bitposition, bool isKeyframe)
		{
			while (buffer.ReadBool(ref bitposition))
			{
				PassThruType passThruType = (PassThruType)buffer.Read(ref bitposition, 3);
				bool flag = passThruType == PassThruType.SetTrigger || passThruType == PassThruType.ResetTrigger;
				int index;
				if (buffer.ReadBool(ref bitposition))
				{
					if (flag)
					{
						index = (int)buffer.Read(ref bitposition, bitsForTriggerIndex);
						index = sharedTriggIndexes[index];
					}
					else
					{
						index = (int)buffer.Read(ref bitposition, bitsForStateIndex);
						index = sharedStateIndexes[index];
					}
				}
				else
				{
					index = buffer.ReadSigned(ref bitposition, 32);
				}
				if (flag)
				{
					frame.passThrus.Enqueue(new AnimPassThru(passThruType, index, -1, -1f, -1f, -1f));
					continue;
				}
				int layer = ((layerCount > 1) ? ((int)buffer.Read(ref bitposition, bitsForLayerIndex) - 1) : (-1));
				float normTime;
				float otherTime;
				if (passThruType == PassThruType.Play || passThruType == PassThruType.CrossFade)
				{
					normTime = (buffer.ReadBool(ref bitposition) ? buffer.ReadNorm(ref bitposition, (int)passthruNormTimeCompress) : 0f);
					otherTime = -1f;
				}
				else
				{
					otherTime = (buffer.ReadBool(ref bitposition) ? buffer.ReadHalf(ref bitposition) : 0f);
					normTime = -1f;
				}
				float duration = ((passThruType == PassThruType.CrossFade || passThruType == PassThruType.CrossFadeFixed) ? buffer.ReadHalf(ref bitposition) : (-1f));
				frame.passThrus.Enqueue(new AnimPassThru(passThruType, index, layer, normTime, otherTime, duration));
			}
		}

		private void CaptureStates(Frame frame)
		{
			int num = ((!syncLayers) ? 1 : layerCount);
			for (int i = 0; i < num; i++)
			{
				frame.layerWeights[i] = animator.GetLayerWeight(i);
				if (animator.IsInTransition(i))
				{
					frame.layerIsInTransition[i] = true;
					continue;
				}
				AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(i);
				frame.layerIsInTransition[i] = false;
				frame.stateHashes[i] = currentAnimatorStateInfo.fullPathHash;
				float normalizedTime = currentAnimatorStateInfo.normalizedTime;
				if (normalizedTimeCompress == NormalizedFloatCompression.Full32 || normalizedTimeCompress == NormalizedFloatCompression.Half16)
				{
					frame.normalizedTime[i] = normalizedTime;
				}
				else if (currentAnimatorStateInfo.loop)
				{
					frame.normalizedTime[i] = ((normalizedTime > 1f) ? (normalizedTime % 1f) : normalizedTime);
				}
				else
				{
					frame.normalizedTime[i] = ((normalizedTime > 1f) ? 1f : ((normalizedTime < 0f) ? 0f : normalizedTime));
				}
			}
		}

		private SerializationFlags WriteStates(Frame frame, byte[] buffer, ref int bitposition, bool isKeyframe)
		{
			int?[] stateHashes = frame.stateHashes;
			float[] normalizedTime = frame.normalizedTime;
			float?[] layerWeights = frame.layerWeights;
			bool[] layerIsInTransition = frame.layerIsInTransition;
			SerializationFlags serializationFlags = SerializationFlags.None;
			int num = ((!syncLayers) ? 1 : layerCount);
			for (int i = 0; i < num; i++)
			{
				int? num2 = stateHashes[i];
				bool flag = !num2.HasValue || lastAnimationHash[i] != num2.Value;
				if (isKeyframe || flag)
				{
					buffer.WriteBool(true, ref bitposition);
					bool flag2 = layerIsInTransition[i];
					buffer.WriteBool(flag2, ref bitposition);
					if (!flag2)
					{
						bool flag3 = sharedStateIndexes.IndexOf(num2.Value) != -1;
						buffer.WriteBool(flag3, ref bitposition);
						if (flag3)
						{
							buffer.Write((uint)sharedStateIndexes.IndexOf(num2.Value), ref bitposition, bitsForStateIndex);
						}
						else
						{
							buffer.WriteSigned(num2.Value, ref bitposition, 32);
						}
						float value = (normalizedTime[i] + 1f) * 0.5f;
						buffer.WriteNorm(value, ref bitposition, (int)normalizedTimeCompress);
					}
					lastAnimationHash[i] = (num2.HasValue ? num2.Value : 0);
					serializationFlags |= SerializationFlags.HasContent;
				}
				else
				{
					buffer.WriteBool(false, ref bitposition);
				}
				if (!syncLayerWeights || i == 0)
				{
					continue;
				}
				float? num3 = layerWeights[i];
				if (num3 == 1f)
				{
					buffer.WriteBool(true, ref bitposition);
					serializationFlags |= SerializationFlags.HasContent;
					continue;
				}
				buffer.WriteBool(false, ref bitposition);
				if (num3 == 0f)
				{
					buffer.WriteBool(true, ref bitposition);
					serializationFlags |= SerializationFlags.HasContent;
					continue;
				}
				buffer.WriteBool(false, ref bitposition);
				int bits = (int)layerWeightCompress;
				uint num4 = num3.Value.CompressNorm(bits);
				if (isKeyframe || lastLayerWeight[i] != num4)
				{
					buffer.WriteBool(true, ref bitposition);
					buffer.Write(num4, ref bitposition, bits);
					lastLayerWeight[i] = num4;
					serializationFlags |= SerializationFlags.HasContent;
				}
				else
				{
					buffer.WriteBool(false, ref bitposition);
				}
			}
			return serializationFlags;
		}

		private void ReadStates(Frame frame, byte[] buffer, ref int bitposition, bool isKeyframe)
		{
			int?[] stateHashes = frame.stateHashes;
			float[] normalizedTime = frame.normalizedTime;
			float?[] layerWeights = frame.layerWeights;
			bool[] layerIsInTransition = frame.layerIsInTransition;
			int num = ((!syncLayers) ? 1 : layerCount);
			for (int i = 0; i < num; i++)
			{
				if (buffer.ReadBool(ref bitposition) && !(layerIsInTransition[i] = buffer.ReadBool(ref bitposition)))
				{
					bool num2 = buffer.ReadBool(ref bitposition);
					int num3 = (num2 ? ((int)buffer.Read(ref bitposition, bitsForStateIndex)) : buffer.ReadSigned(ref bitposition, 32));
					if (num2)
					{
						num3 = sharedStateIndexes[num3];
					}
					stateHashes[i] = num3;
					float num4 = buffer.ReadNorm(ref bitposition, (int)normalizedTimeCompress) * 2f - 1f;
					normalizedTime[i] = num4;
				}
				if (syncLayerWeights && i != 0)
				{
					if (buffer.ReadBool(ref bitposition))
					{
						layerWeights[i] = 1f;
					}
					else if (buffer.ReadBool(ref bitposition))
					{
						layerWeights[i] = 0f;
					}
					else if (buffer.ReadBool(ref bitposition))
					{
						layerWeights[i] = buffer.ReadNorm(ref bitposition, (int)layerWeightCompress);
					}
					else
					{
						layerWeights[i] = null;
					}
				}
			}
		}

		private void ApplyState(Frame applyFrame)
		{
			int num = ((!syncLayers) ? 1 : layerCount);
			for (int i = 0; i < num; i++)
			{
				int? num2 = applyFrame.stateHashes[i];
				bool flag = applyFrame.layerIsInTransition[i];
				if (!num2.HasValue)
				{
					continue;
				}
				if (!flag && num2.Value != 0)
				{
					animator.Play(num2.Value, i, applyFrame.normalizedTime[i]);
				}
				if (syncLayerWeights)
				{
					float? num3 = applyFrame.layerWeights[i];
					if (num3.HasValue)
					{
						animator.SetLayerWeight(i, num3.Value);
					}
				}
			}
		}

		private void CaptureLayerWeights(Frame frame)
		{
			int num = ((!syncLayers) ? 1 : layerCount);
			for (int i = 0; i < num; i++)
			{
				frame.layerWeights[i] = animator.GetLayerWeight(i);
			}
		}

		private SerializationFlags WriteLayerWeights(Frame frame, byte[] buffer, ref int bitposition, bool isKeyframe)
		{
			float?[] layerWeights = frame.layerWeights;
			SerializationFlags serializationFlags = SerializationFlags.None;
			int num = ((!syncLayers) ? 1 : layerCount);
			for (int i = 0; i < num; i++)
			{
				if (!syncLayerWeights || i == 0)
				{
					continue;
				}
				float? num2 = layerWeights[i];
				if (num2 == 1f)
				{
					buffer.WriteBool(true, ref bitposition);
					serializationFlags |= SerializationFlags.HasContent;
					continue;
				}
				buffer.WriteBool(false, ref bitposition);
				if (num2 == 0f)
				{
					buffer.WriteBool(true, ref bitposition);
					serializationFlags |= SerializationFlags.HasContent;
					continue;
				}
				buffer.WriteBool(false, ref bitposition);
				int bits = (int)layerWeightCompress;
				uint num3 = num2.Value.CompressNorm(bits);
				if (isKeyframe || lastLayerWeight[i] != num3)
				{
					buffer.WriteBool(true, ref bitposition);
					buffer.Write(num3, ref bitposition, bits);
					lastLayerWeight[i] = num3;
					serializationFlags |= SerializationFlags.HasContent;
				}
				else
				{
					buffer.WriteBool(false, ref bitposition);
				}
			}
			return serializationFlags;
		}

		private SerializationFlags ReadLayerWeights(Frame frame, byte[] buffer, ref int bitposition, bool isKeyframe)
		{
			float?[] layerWeights = frame.layerWeights;
			SerializationFlags serializationFlags = SerializationFlags.None;
			int num = ((!syncLayers) ? 1 : layerCount);
			for (int i = 0; i < num; i++)
			{
				if (syncLayerWeights && i != 0)
				{
					if (buffer.ReadBool(ref bitposition))
					{
						layerWeights[i] = 1f;
						serializationFlags |= SerializationFlags.HasContent;
					}
					else if (buffer.ReadBool(ref bitposition))
					{
						layerWeights[i] = 0f;
						serializationFlags |= SerializationFlags.HasContent;
					}
					else if (buffer.ReadBool(ref bitposition))
					{
						layerWeights[i] = buffer.ReadNorm(ref bitposition, (int)layerWeightCompress);
						serializationFlags |= SerializationFlags.HasContent;
					}
					else
					{
						layerWeights[i] = null;
					}
				}
			}
			return SerializationFlags.HasContent;
		}

		private void ApplyLayerWeights(Frame applyFrame)
		{
			int num = ((!syncLayers) ? 1 : layerCount);
			for (int i = 0; i < num; i++)
			{
				if (syncLayerWeights)
				{
					float? num2 = applyFrame.layerWeights[i];
					if (num2.HasValue)
					{
						animator.SetLayerWeight(i, num2.Value);
					}
				}
			}
		}

		public override bool OnSnapshot(int prevFrameId, int snapFrameId, int targFrameId, bool prevIsValid, bool snapIsValid, bool targIsValid)
		{
			if (snapFrame != null && snapFrame.content != 0)
			{
				ApplyFrame(snapFrame);
			}
			if (!base.OnSnapshot(prevFrameId, snapFrameId, targFrameId, prevIsValid, snapIsValid, targIsValid))
			{
				return false;
			}
			CompleteTargetParameters();
			return true;
		}

		private void ApplyFrame(Frame frame)
		{
			if (syncStates)
			{
				ApplyState(frame);
			}
			if (syncLayerWeights)
			{
				ApplyLayerWeights(frame);
			}
			ExecutePassThruQueue(frame);
			InterpolateParams(0f);
		}

		public override bool OnInterpolate(int snapFrameId, int targFrameId, float t)
		{
			if (!base.OnInterpolate(snapFrameId, targFrameId, t))
			{
				return false;
			}
			if (targFrame == null)
			{
				return false;
			}
			if (targFrame.content == FrameContents.Empty)
			{
				return false;
			}
			if (syncParams)
			{
				InterpolateParams(t);
			}
			return true;
		}

		protected override void InterpolateFrame(Frame targframe, Frame startframe, Frame endframe, float t)
		{
			targframe.CopyFrom(endframe);
			InterpolateState(targframe, startframe, endframe, t);
		}

		protected override void ExtrapolateFrame(Frame prevframe, Frame snapframe, Frame targframe)
		{
			targframe.content = FrameContents.Partial;
			ExtrapolateParams(prevframe, snapframe, targframe);
			ExtrapolateState();
		}

		private void ExtrapolateState()
		{
			int num = ((!syncLayers) ? 1 : layerCount);
			for (int i = 0; i < num; i++)
			{
				targFrame.stateHashes[i] = null;
			}
		}

		private void InterpolateState(Frame targFrame, Frame strFrame, Frame endFrame, float t)
		{
			int num = ((!syncLayers) ? 1 : layerCount);
			for (int i = 0; i < num; i++)
			{
				int? num2 = strFrame.stateHashes[i];
				int? num3 = endFrame.stateHashes[i];
				targFrame.stateHashes[i] = num3;
				float num4 = strFrame.normalizedTime[i];
				float b = endFrame.normalizedTime[i];
				if (num2 != num3 && num4 != 0f)
				{
					targFrame.normalizedTime[i] = Mathf.LerpUnclamped(num4, b, t);
				}
				else
				{
					targFrame.normalizedTime[i] = num4;
				}
			}
		}
	}
}
