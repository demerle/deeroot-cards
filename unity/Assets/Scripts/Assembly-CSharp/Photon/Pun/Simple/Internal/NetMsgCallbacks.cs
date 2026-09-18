using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using UnityEngine;

namespace Photon.Pun.Simple.Internal
{
	public static class NetMsgCallbacks
	{
		public delegate void ByteBufferCallback(object conn, int connId, byte[] buffer);

		private class CallbackLists
		{
			public List<ByteBufferCallback> bufferCallbacks;
		}

		private static Dictionary<int, CallbackLists> callbacks = new Dictionary<int, CallbackLists>();

		public const byte DEF_MSG_ID = 215;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void RegisterOnEventListener()
		{
			PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
		}

		public static void OnEvent(EventData photonEvent)
		{
			byte code = photonEvent.Code;
			if (!callbacks.ContainsKey(code))
			{
				return;
			}
			bool useByteArraySlicePoolForEvents = PhotonNetwork.NetworkingClient.LoadBalancingPeer.UseByteArraySlicePoolForEvents;
			ByteArraySlice byteArraySlice;
			byte[] buffer;
			if (useByteArraySlicePoolForEvents)
			{
				byteArraySlice = photonEvent.CustomData as ByteArraySlice;
				buffer = byteArraySlice.Buffer;
			}
			else
			{
				byteArraySlice = null;
				buffer = photonEvent.CustomData as byte[];
			}
			CallbackLists callbackLists = callbacks[code];
			if (callbackLists.bufferCallbacks != null && callbackLists.bufferCallbacks.Count > 0)
			{
				foreach (ByteBufferCallback bufferCallback in callbackLists.bufferCallbacks)
				{
					bufferCallback(null, photonEvent.Sender, buffer);
				}
			}
			if (useByteArraySlicePoolForEvents)
			{
				byteArraySlice.Release();
			}
		}

		[Obsolete("Removed the asServer from UNET side, killing it here as well.")]
		public static void RegisterCallback(byte msgid, ByteBufferCallback callback, bool asServer)
		{
			if (!callbacks.ContainsKey(msgid))
			{
				callbacks.Add(msgid, new CallbackLists());
			}
			if (callbacks[msgid].bufferCallbacks == null)
			{
				callbacks[msgid].bufferCallbacks = new List<ByteBufferCallback>();
			}
			List<ByteBufferCallback> bufferCallbacks = callbacks[msgid].bufferCallbacks;
			if (!bufferCallbacks.Contains(callback))
			{
				bufferCallbacks.Add(callback);
			}
		}

		public static void RegisterCallback(ByteBufferCallback callback)
		{
			RegisterCallback(215, callback);
		}

		public static void RegisterCallback(byte msgid, ByteBufferCallback callback)
		{
			if (!callbacks.ContainsKey(msgid))
			{
				callbacks.Add(msgid, new CallbackLists());
			}
			if (callbacks[msgid].bufferCallbacks == null)
			{
				callbacks[msgid].bufferCallbacks = new List<ByteBufferCallback>();
			}
			List<ByteBufferCallback> bufferCallbacks = callbacks[msgid].bufferCallbacks;
			if (!bufferCallbacks.Contains(callback))
			{
				bufferCallbacks.Add(callback);
			}
		}

		[Obsolete("Removed the asServer from UNET side, killing it here as well.")]
		public static void UnregisterCallback(byte msgid, ByteBufferCallback callback, bool asServer)
		{
			if (callbacks.ContainsKey(msgid))
			{
				CallbackLists callbackLists = callbacks[msgid];
				callbackLists.bufferCallbacks.Remove(callback);
				if (callbackLists.bufferCallbacks.Count == 0)
				{
					callbacks.Remove(msgid);
				}
			}
		}

		public static void UnregisterCallback(ByteBufferCallback callback)
		{
			UnregisterCallback(215, callback);
		}

		public static void UnregisterCallback(byte msgid, ByteBufferCallback callback)
		{
			if (callbacks.ContainsKey(msgid))
			{
				CallbackLists callbackLists = callbacks[msgid];
				callbackLists.bufferCallbacks.Remove(callback);
				if (callbackLists.bufferCallbacks.Count == 0)
				{
					callbacks.Remove(msgid);
				}
			}
		}
	}
}
