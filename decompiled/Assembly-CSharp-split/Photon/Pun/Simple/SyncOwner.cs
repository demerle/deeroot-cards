using Photon.Compression;

namespace Photon.Pun.Simple;

public class SyncOwner : SyncObject<SyncOwner.Frame>, IOnCaptureState, IOnNetSerialize, IOnSnapshot, IUseKeyframes, IOnIncrementFrame
{
	public class Frame : FrameBase
	{
		public int ownerActorId;

		public bool ownerHasChanged;

		public override void Clear()
		{
			ownerActorId = -1;
			ownerHasChanged = false;
			base.Clear();
		}

		public override void CopyFrom(FrameBase sourceFrame)
		{
			base.CopyFrom(sourceFrame);
			Frame frame = sourceFrame as Frame;
			ownerActorId = frame.ownerActorId;
			ownerHasChanged = false;
		}
	}

	public bool reliableOwnerChange = true;

	protected bool pendingOwnerChange;

	protected int pendingOwnerId = -1;

	protected int ticksUntilOwnershipRetry = -1;

	public override int ApplyOrder => 21;

	public override void OnAuthorityChanged(bool isMine, bool controllerChanged)
	{
		base.OnAuthorityChanged(isMine, controllerChanged);
		if (!isMine)
		{
			pendingOwnerChange = false;
			ticksUntilOwnershipRetry = -1;
		}
	}

	public void TransferOwner(int newOwnerId)
	{
		if (photonView.IsMine)
		{
			pendingOwnerChange = true;
			pendingOwnerId = newOwnerId;
		}
	}

	public void OnCaptureCurrentState(int frameId)
	{
		Frame frame = frames[frameId];
		if (pendingOwnerChange)
		{
			if (photonView.OwnerActorNr != 0)
			{
				ticksUntilOwnershipRetry = TickEngineSettings.frameCount;
			}
			NetMasterCallbacks.postCallbackActions.Enqueue(DeferredOwnerChange);
			frame.ownerActorId = pendingOwnerId;
			frame.ownerHasChanged = true;
			pendingOwnerChange = false;
		}
		else
		{
			frames[frameId].ownerActorId = photonView.OwnerActorNr;
		}
	}

	public SerializationFlags OnNetSerialize(int frameId, byte[] buffer, ref int bitposition, SerializationFlags writeFlags)
	{
		Frame frame = frames[frameId];
		bool flag = frame.ownerHasChanged && (reliableOwnerChange || keyframeRate == 0 || (writeFlags & SerializationFlags.NewConnection) != 0);
		if (!flag && !IsKeyframe(frameId))
		{
			buffer.Write(0uL, ref bitposition, 1);
			return SerializationFlags.None;
		}
		buffer.Write(1uL, ref bitposition, 1);
		SerializationFlags serializationFlags = SerializationFlags.HasContent;
		buffer.WritePackedBytes((uint)frame.ownerActorId, ref bitposition, 32);
		if (flag)
		{
			serializationFlags |= SerializationFlags.ForceReliable;
		}
		return serializationFlags;
	}

	public SerializationFlags OnNetDeserialize(int originFrameId, byte[] buffer, ref int bitposition, FrameArrival frameArrival)
	{
		Frame frame = frames[originFrameId];
		bool flag = buffer.Read(ref bitposition, 1) != 0;
		int ownerActorId = (flag ? ((int)buffer.ReadPackedBytes(ref bitposition, 32)) : (-1));
		if (photonView.IsMine)
		{
			return SerializationFlags.None;
		}
		if (flag)
		{
			frame.content = FrameContents.Complete;
			frame.ownerActorId = ownerActorId;
			return SerializationFlags.HasContent;
		}
		frame.content = FrameContents.Empty;
		return SerializationFlags.None;
	}

	protected override void ApplySnapshot(Frame snapframe, Frame targframe, bool snapIsValid, bool targIsValid)
	{
		if (snapIsValid && snapframe.content == FrameContents.Complete)
		{
			int ownerActorId = snapframe.ownerActorId;
			pendingOwnerId = ownerActorId;
			NetMasterCallbacks.postCallbackActions.Enqueue(DeferredOwnerChange);
			ticksUntilOwnershipRetry = -1;
		}
	}

	protected void DeferredOwnerChange()
	{
		PhotonNetwork.CurrentRoom.Players.TryGetValue(pendingOwnerId, out var value);
		photonView.SetOwnerInternal(value, pendingOwnerId);
	}

	public void OnIncrementFrame(int newFrameId, int newSubFrameId, int previousFrameId, int prevSubFrameId)
	{
		if (!photonView.IsMine && newSubFrameId == 0 && ticksUntilOwnershipRetry >= 0)
		{
			if (ticksUntilOwnershipRetry == 0)
			{
				Debug.LogError(base.name + " FALLBACK OWNER CHANGE " + photonView.ControllerActorNr);
				photonView.TransferOwnership(photonView.Controller);
				ticksUntilOwnershipRetry = TickEngineSettings.frameCount;
			}
			else
			{
				ticksUntilOwnershipRetry--;
			}
		}
	}
}
