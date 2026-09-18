using ExitGames.Client.Photon;
using Photon.Compression;
using Photon.Pun.Simple.Internal;
using Photon.Utilities;
using UnityEngine;

namespace Photon.Pun.Simple
{
	[HelpURL("https://doc.photonengine.com/en-us/pun/current/gameplay/simple/simpleoverview")]
	public class NetMaster : MonoBehaviour
	{
		public static NetMaster single;

		public static bool isShuttingDown;

		protected static float lastSentTickTime;

		private static int _currFrameId;

		private static int _currSubFrameId;

		private static int _prevFrameId;

		private static int _prevSubFrameId;

		protected static float rtt;

		private bool simulationHasRun;

		public const int BITS_FOR_NETOBJ_SIZE = 16;

		public static int CurrentFrameId
		{
			get
			{
				return _currFrameId;
			}
		}

		public static int CurrentSubFrameId
		{
			get
			{
				return _currSubFrameId;
			}
		}

		public static int PreviousFrameId
		{
			get
			{
				return _prevFrameId;
			}
		}

		public static int PreviousSubFrameId
		{
			get
			{
				return _prevSubFrameId;
			}
		}

		public static float NormTimeSinceFixed { get; private set; }

		public static float RTT
		{
			get
			{
				return rtt;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void EnsureExistsInScene()
		{
			if (!SettingsScriptableObject<TickEngineSettings>.Single.enableTickEngine)
			{
				return;
			}
			GameObject gameObject = null;
			if ((bool)single)
			{
				gameObject = single.gameObject;
			}
			else
			{
				if ((bool)NetMasterLate.single)
				{
					gameObject = NetMasterLate.single.gameObject;
				}
				single = Object.FindObjectOfType<NetMaster>();
				if ((bool)single)
				{
					gameObject = single.gameObject;
				}
				else
				{
					if (!gameObject)
					{
						gameObject = new GameObject("Net Master");
					}
					single = gameObject.AddComponent<NetMaster>();
				}
			}
			if (!NetMasterLate.single)
			{
				NetMasterLate.single = Object.FindObjectOfType<NetMasterLate>();
				if (!NetMasterLate.single)
				{
					NetMasterLate.single = gameObject.AddComponent<NetMasterLate>();
				}
			}
			NetMsgCallbacks.RegisterCallback(ReceiveMessage);
		}

		private void Awake()
		{
			if ((bool)single && single != this)
			{
				Object.Destroy(single);
			}
			single = this;
			Object.DontDestroyOnLoad(this);
			_prevFrameId = TickEngineSettings.frameCount - 1;
			_prevSubFrameId = TickEngineSettings.sendEveryXTick - 1;
		}

		private void OnApplicationQuit()
		{
			isShuttingDown = true;
			NetMasterCallbacks.OnPreQuitCallbacks();
		}

		private void FixedUpdate()
		{
			if ((NetObject.activeControlledNetObjs.Count == 0 && NetObject.activeUncontrolledNetObjs.Count == 0) || !SettingsScriptableObject<TickEngineSettings>.single.enableTickEngine)
			{
				return;
			}
			if (!NetMsgSends.ReadyToSend)
			{
				DoubleTime.SnapFixed();
				return;
			}
			if (simulationHasRun)
			{
				PostSimulate();
			}
			DoubleTime.SnapFixed();
			bool flag = true;
			while (PhotonNetwork.InRoom && PhotonNetwork.IsMessageQueueRunning && flag)
			{
				flag = PhotonNetwork.NetworkingClient.LoadBalancingPeer.DispatchIncomingCommands();
			}
			rtt = (float)PhotonNetwork.GetPing() * 0.001f;
			simulationHasRun = true;
		}

		private void Update()
		{
			if ((NetObject.activeControlledNetObjs.Count != 0 || NetObject.activeUncontrolledNetObjs.Count != 0) && SettingsScriptableObject<TickEngineSettings>.single.enableTickEngine)
			{
				if (simulationHasRun)
				{
					PostSimulate();
				}
				DoubleTime.SnapUpdate();
				NormTimeSinceFixed = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
				NetMasterCallbacks.OnPreUpdateCallbacks();
				float t = (Time.time - lastSentTickTime) / TickEngineSettings.netTickInterval;
				NetObject.NetObjDictsLocked = true;
				NonAllocDictionary<int, NetObject>.ValueIterator enumerator = NetObject.activeUncontrolledNetObjs.Values.GetEnumerator();
				while (enumerator.MoveNext())
				{
					enumerator.Current.OnInterpolate(_prevFrameId, _currFrameId, t);
				}
				NetObject.NetObjDictsLocked = false;
				NetMasterCallbacks.OnInterpolateCallbacks(_prevFrameId, _currFrameId, t);
			}
		}

		private void LateUpdate()
		{
			NetMasterCallbacks.OnPreLateUpdateCallbacks();
		}

		private void PostSimulate()
		{
			bool flag = _currSubFrameId == TickEngineSettings.sendEveryXTick - 1;
			NetMasterCallbacks.OnPostSimulateCallbacks(_currFrameId, _currSubFrameId, flag);
			if (flag)
			{
				SerializeAllAndSend();
			}
			IncrementFrameId();
			simulationHasRun = false;
		}

		private static void IncrementFrameId()
		{
			_prevSubFrameId = _currSubFrameId;
			_currSubFrameId++;
			if (_currSubFrameId >= TickEngineSettings.sendEveryXTick)
			{
				_currSubFrameId = 0;
				_prevFrameId = _currFrameId;
				_currFrameId++;
				if (_currFrameId >= TickEngineSettings.frameCount)
				{
					_currFrameId = 0;
				}
			}
			NetMasterCallbacks.OnIncrementFrameCallbacks(_currFrameId, _currSubFrameId, _prevFrameId, _prevSubFrameId);
			if (_currSubFrameId == 0)
			{
				TickManager.PreSnapshot(_currFrameId);
				NetObject.NetObjDictsLocked = true;
				NonAllocDictionary<int, NetObject>.ValueIterator enumerator = NetObject.activeUncontrolledNetObjs.Values.GetEnumerator();
				while (enumerator.MoveNext())
				{
					enumerator.Current.OnSnapshot(_currFrameId);
				}
				NetObject.NetObjDictsLocked = false;
				NetMasterCallbacks.OnSnapshotCallbacks(_currFrameId);
				TickManager.PostSnapshot(_currFrameId);
				lastSentTickTime = Time.fixedTime;
			}
		}

		private static void SerializeAllAndSend()
		{
			byte[] reusableBuffer = NetMsgSends.reusableBuffer;
			int bitposition = 0;
			SerializationFlags writeFlags;
			SerializationFlags flags;
			if (TickManager.needToSendInitialForNewConn)
			{
				writeFlags = (SerializationFlags)22;
				flags = SerializationFlags.HasContent;
				TickManager.needToSendInitialForNewConn = false;
			}
			else
			{
				writeFlags = SerializationFlags.None;
				flags = SerializationFlags.None;
			}
			reusableBuffer.Write((uint)_currFrameId, ref bitposition, TickEngineSettings.frameCountBits);
			NetMasterCallbacks.OnPreSerializeTickCallbacks(_currFrameId, reusableBuffer, ref bitposition);
			NetObject.NetObjDictsLocked = true;
			SerializeNetObjDict(NetObject.activeControlledNetObjs, reusableBuffer, ref bitposition, ref flags, writeFlags);
			NetObject.NetObjDictsLocked = false;
			while (NetMasterCallbacks.postSerializationActions.Count > 0)
			{
				NetMasterCallbacks.postSerializationActions.Dequeue()();
			}
			if (flags != 0)
			{
				reusableBuffer.WritePackedBytes(0uL, ref bitposition, 32);
				reusableBuffer.Send(bitposition, null, flags, true);
			}
		}

		private static void SerializeNetObjDict(NonAllocDictionary<int, NetObject> dict, byte[] buffer, ref int bitposition, ref SerializationFlags flags, SerializationFlags writeFlags)
		{
			NonAllocDictionary<int, NetObject>.ValueIterator enumerator = dict.Values.GetEnumerator();
			while (enumerator.MoveNext())
			{
				NetObject current = enumerator.Current;
				int num = bitposition;
				buffer.WritePackedBytes((uint)current.ViewID, ref bitposition, 32);
				int num2 = bitposition;
				buffer.WriteBool(true, ref bitposition);
				int bitposition2 = bitposition;
				bitposition += 16;
				SerializationFlags serializationFlags = current.OnNetSerialize(_currFrameId, buffer, ref bitposition, writeFlags);
				if (serializationFlags == SerializationFlags.None)
				{
					if (current.SkipWhenEmpty)
					{
						bitposition = num;
						continue;
					}
					bitposition = num2;
					buffer.WriteBool(false, ref bitposition);
				}
				else
				{
					flags |= serializationFlags;
					int num3 = bitposition - bitposition2;
					buffer.Write((uint)num3, ref bitposition2, 16);
				}
			}
		}

		public static void ReceiveMessage(object conn, int connId, byte[] buffer)
		{
			int frameCount = TickEngineSettings.frameCount;
			int bitposition = 0;
			int originFrameId = (int)buffer.Read(ref bitposition, TickEngineSettings.frameCountBits);
			FrameArrival arrival;
			TickManager.LogIncomingFrame(connId, originFrameId, out arrival);
			while (true)
			{
				int num = (int)buffer.ReadPackedBytes(ref bitposition, 32);
				if (num == 0)
				{
					break;
				}
				bool flag = buffer.ReadBool(ref bitposition);
				if (!flag)
				{
					continue;
				}
				int num2 = bitposition;
				int num3 = (int)buffer.Read(ref bitposition, 16);
				int num4 = num2 + num3;
				PhotonView photonView = PhotonNetwork.GetPhotonView(num);
				NetObject netObject = (photonView ? photonView.GetComponent<NetObject>() : null);
				if ((object)netObject == null)
				{
					bitposition = num4;
					continue;
				}
				if (netObject.IgnoreNonControllerUpdates)
				{
					int controllerActorNr = photonView.ControllerActorNr;
					int ownerActorNr = photonView.OwnerActorNr;
					if (controllerActorNr == -1)
					{
						photonView.SetControllerInternal(connId);
					}
					else if (controllerActorNr != connId && ownerActorNr != connId)
					{
						bitposition = num4;
						continue;
					}
				}
				netObject.OnDeserialize(connId, originFrameId, buffer, ref bitposition, flag, arrival);
				bitposition = num4;
			}
		}

		public static FrameArrival CheckFrameArrival(int incomingFrame)
		{
			int num = incomingFrame - _prevFrameId;
			if (num == 0)
			{
				return FrameArrival.IsSnap;
			}
			if (num < 0)
			{
				num += TickEngineSettings.frameCount;
			}
			if (num == 1)
			{
				return FrameArrival.IsTarget;
			}
			if (num >= TickEngineSettings.halfFrameCount)
			{
				return FrameArrival.IsLate;
			}
			return FrameArrival.IsFuture;
		}
	}
}
