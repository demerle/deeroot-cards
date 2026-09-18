using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine;

namespace Photon.Pun.Simple.Internal;

public class TickManager : IInRoomCallbacks
{
	public static readonly Dictionary<int, ConnectionTickOffsets> perConnOffsets = new Dictionary<int, ConnectionTickOffsets>();

	public static readonly List<int> connections = new List<int>();

	public static TickManager single;

	public static bool needToSendInitialForNewConn;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	public static void Bootstrap()
	{
		single = new TickManager();
		PhotonNetwork.NetworkingClient.AddCallbackTarget(single);
	}

	public void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
	{
		AddConnection(newPlayer.ActorNumber);
	}

	public void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
	{
		RemoveConnection(otherPlayer.ActorNumber);
	}

	public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
	{
	}

	public void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
	{
	}

	public void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
	{
	}

	public static void PreSnapshot(int currentFrameId)
	{
		for (int i = 0; i < connections.Count; i++)
		{
			if (perConnOffsets[connections[i]] != null)
			{
				ConnectionTickOffsets connectionTickOffsets = perConnOffsets[connections[i]];
				int num = connectionTickOffsets.ConvertFrameLocalToOrigin(currentFrameId);
				float num2 = (connectionTickOffsets.validFrameMask[num] ? (Time.time - connectionTickOffsets.frameArriveTime[num]) : (-1f));
				connectionTickOffsets.frameTimeBeforeConsumption[num] = num2;
				connectionTickOffsets.SnapshotAdvance();
			}
		}
	}

	public static void PostSnapshot(int currentFrameId)
	{
		for (int i = 0; i < connections.Count; i++)
		{
			if (perConnOffsets[connections[i]] != null)
			{
				perConnOffsets[connections[i]].PostSnapshot();
			}
		}
	}

	public static ConnectionTickOffsets LogIncomingFrame(int connId, int originFrameId, out FrameArrival arrival)
	{
		int frameCount = TickEngineSettings.frameCount;
		if (!perConnOffsets.TryGetValue(connId, out var value) || value == null)
		{
			LogNewConnection(connId, originFrameId, frameCount, out value);
		}
		int num = originFrameId + value.originToLocalFrame;
		if (num >= frameCount)
		{
			num -= frameCount;
		}
		value.frameArriveTime[originFrameId] = Time.time;
		int currentFrameId = NetMaster.CurrentFrameId;
		int num2;
		if (num == currentFrameId)
		{
			num2 = 0;
		}
		else
		{
			num2 = currentFrameId - num;
			if (num2 < 0)
			{
				num2 += frameCount;
			}
			if (num2 >= TickEngineSettings.halfFrameCount)
			{
				num2 -= frameCount;
			}
		}
		if (num2 >= 0)
		{
			if (num2 != 0 && num2 != 1 && num2 < TickEngineSettings.halfFrameCount)
			{
			}
		}
		else
		{
			_ = -TickEngineSettings.halfFrameCount;
		}
		arrival = (FrameArrival)num2;
		bool flag = num2 <= 0;
		value.frameArrivedTooLate |= !flag;
		value.validFrameMask.Set(originFrameId, value: true);
		return value;
	}

	private static void LogNewConnection(int connId, int originFrameId, int frameCount, out ConnectionTickOffsets offsets)
	{
		int num;
		for (num = NetMaster.CurrentFrameId + TickEngineSettings.targetBufferSize; num >= frameCount; num -= frameCount)
		{
		}
		int num2 = num - originFrameId;
		if (num2 < 0)
		{
			num2 += frameCount;
		}
		int num3 = frameCount - num2;
		if (num3 < 0)
		{
			num3 += frameCount;
		}
		offsets = new ConnectionTickOffsets(connId, num2, num3);
		AddConnection(connId, offsets);
	}

	private static void AddConnection(int connId, ConnectionTickOffsets offsets = null)
	{
		if (PhotonNetwork.LocalPlayer.ActorNumber != connId)
		{
			if (!connections.Contains(connId))
			{
				perConnOffsets.Add(connId, offsets);
				connections.Add(connId);
				NetMsgSends.newPlayers.Add(connId);
				needToSendInitialForNewConn = true;
			}
			else
			{
				perConnOffsets[connId] = offsets;
			}
		}
	}

	public static void RemoveConnection(int connId)
	{
		if (perConnOffsets.ContainsKey(connId))
		{
			perConnOffsets.Remove(connId);
			connections.Remove(connId);
		}
	}
}
