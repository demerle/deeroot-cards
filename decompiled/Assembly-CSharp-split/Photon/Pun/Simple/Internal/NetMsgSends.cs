using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Compression;
using Photon.Realtime;
using Photon.Utilities;
using UnityEngine;

namespace Photon.Pun.Simple.Internal;

public static class NetMsgSends
{
	private static bool unreliableCapable;

	public static byte[] reusableBuffer = new byte[16384];

	public static byte[] reusableNetObjBuffer = new byte[4096];

	public static HashSet<int> newPlayers = new HashSet<int>();

	private static RaiseEventOptions[] opts = new RaiseEventOptions[3]
	{
		new RaiseEventOptions
		{
			Receivers = ReceiverGroup.Others
		},
		new RaiseEventOptions
		{
			Receivers = ReceiverGroup.All
		},
		new RaiseEventOptions
		{
			Receivers = ReceiverGroup.MasterClient
		}
	};

	public static bool ReadyToSend => PhotonNetwork.NetworkClientState == ClientState.Joined;

	public static bool AmActiveServer => false;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void CacheSettings()
	{
		unreliableCapable = PhotonNetwork.NetworkingClient.LoadBalancingPeer.UsedProtocol == ConnectionProtocol.Udp;
	}

	public static void Send(this byte[] buffer, int bitposition, Object refObj, SerializationFlags flags, bool flush = false)
	{
		Room currentRoom = PhotonNetwork.CurrentRoom;
		if (PhotonNetwork.OfflineMode || currentRoom == null || currentRoom.Players == null)
		{
			return;
		}
		bool flag = (flags & SerializationFlags.SendToSelf) != 0;
		if (flag || SettingsScriptableObject<TickEngineSettings>.single.sendWhenSolo || currentRoom.Players.Count > 1)
		{
			ReceiveGroup receiveGroup = (flag ? ReceiveGroup.All : ReceiveGroup.Others);
			int count = bitposition + 7 >> 3;
			LoadBalancingClient networkingClient = PhotonNetwork.NetworkingClient;
			DeliveryMode deliveryMode;
			if (newPlayers.Count > 0)
			{
				deliveryMode = DeliveryMode.Reliable;
				newPlayers.Clear();
			}
			else
			{
				bool flag2 = (flags & SerializationFlags.ForceReliable) != 0;
				deliveryMode = ((!unreliableCapable) ? DeliveryMode.Reliable : (flag2 ? DeliveryMode.ReliableUnsequenced : DeliveryMode.Unreliable));
			}
			SendOptions sendOptions = new SendOptions
			{
				DeliveryMode = deliveryMode
			};
			ByteArraySlice customEventContent = PhotonNetwork.NetworkingClient.LoadBalancingPeer.ByteArraySlicePool.Acquire(buffer, 0, count);
			networkingClient.OpRaiseEvent(215, customEventContent, opts[(int)receiveGroup], sendOptions);
			if (flush)
			{
				networkingClient.Service();
			}
		}
	}
}
