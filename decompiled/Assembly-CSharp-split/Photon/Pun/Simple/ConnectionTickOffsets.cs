using Photon.Utilities;

namespace Photon.Pun.Simple;

public class ConnectionTickOffsets
{
	public int connId;

	public int originToLocalFrame;

	public int localToOriginFrame;

	public int numOfSequentialFramesWithTooSmallBuffer;

	public int numOfSequentialFramesWithTooLargeBuffer;

	public bool frameArrivedTooLate;

	public bool hadInitialSnapshot;

	public int advanceCount;

	public float[] frameArriveTime;

	public float[] frameTimeBeforeConsumption;

	public FastBitMask128 validFrameMask;

	public int ConvertFrameLocalToOrigin(int localFrameId)
	{
		int frameCount = TickEngineSettings.frameCount;
		int num = localFrameId + localToOriginFrame;
		if (num >= frameCount)
		{
			num -= frameCount;
		}
		return num;
	}

	public int ConvertFrameOriginToLocal(int originFrameId)
	{
		int frameCount = TickEngineSettings.frameCount;
		int num = originFrameId + originToLocalFrame;
		if (num >= frameCount)
		{
			num -= frameCount;
		}
		return num;
	}

	public ConnectionTickOffsets(int connId, int originToLocal, int localToOrigin)
	{
		this.connId = connId;
		originToLocalFrame = originToLocal;
		localToOriginFrame = localToOrigin;
		int frameCount = TickEngineSettings.frameCount;
		validFrameMask = new FastBitMask128(frameCount + 1);
		frameArriveTime = new float[frameCount];
		frameTimeBeforeConsumption = new float[frameCount];
		for (int i = 0; i < frameCount; i++)
		{
			frameTimeBeforeConsumption[i] = float.PositiveInfinity;
		}
	}

	public void SnapshotAdvance()
	{
		int frameCount = TickEngineSettings.frameCount;
		int num = NetMaster.CurrentFrameId + localToOriginFrame;
		if (num >= frameCount)
		{
			num -= frameCount;
		}
		int num2 = validFrameMask.CountValidRange(num, TickEngineSettings.quaterFrameCount);
		if (!hadInitialSnapshot)
		{
			if (num2 == 0)
			{
				advanceCount = 0;
				return;
			}
			if (num2 > TickEngineSettings.targetBufferSize)
			{
				advanceCount = num2 - TickEngineSettings.targetBufferSize;
				return;
			}
		}
		if (num2 == 0)
		{
			if (frameArrivedTooLate)
			{
				numOfSequentialFramesWithTooLargeBuffer = 0;
				numOfSequentialFramesWithTooSmallBuffer = 0;
				frameArrivedTooLate = false;
				advanceCount = 0;
				return;
			}
			numOfSequentialFramesWithTooLargeBuffer = 0;
			advanceCount = 1;
		}
		else if (num2 < TickEngineSettings.minBufferSize)
		{
			numOfSequentialFramesWithTooLargeBuffer = 0;
			numOfSequentialFramesWithTooSmallBuffer += ((!frameArrivedTooLate) ? 1 : 2);
			frameArrivedTooLate = false;
			if (numOfSequentialFramesWithTooSmallBuffer >= TickEngineSettings.ticksBeforeGrow)
			{
				advanceCount = 0;
				return;
			}
			advanceCount = 1;
		}
		else if (num2 > TickEngineSettings.maxBufferSize)
		{
			numOfSequentialFramesWithTooSmallBuffer = 0;
			if (numOfSequentialFramesWithTooLargeBuffer > TickEngineSettings.ticksBeforeGrow)
			{
				advanceCount = num2 - TickEngineSettings.targetBufferSize + 1;
				numOfSequentialFramesWithTooLargeBuffer = 0;
			}
			else
			{
				advanceCount = 1;
				numOfSequentialFramesWithTooLargeBuffer++;
			}
		}
		else
		{
			numOfSequentialFramesWithTooLargeBuffer = 0;
			numOfSequentialFramesWithTooSmallBuffer = (frameArrivedTooLate ? 1 : 0);
			advanceCount = 1;
		}
		frameArrivedTooLate = false;
	}

	public void PostSnapshot()
	{
		int frameCount = TickEngineSettings.frameCount;
		int num = NetMaster.CurrentFrameId + localToOriginFrame;
		if (num >= frameCount)
		{
			num -= frameCount;
		}
		int num2 = num - TickEngineSettings.quaterFrameCount;
		if (num2 < 0)
		{
			num2 += frameCount;
		}
		validFrameMask.ClearBitsBefore(num2, TickEngineSettings.quaterFrameCount);
		if (advanceCount > 0)
		{
			hadInitialSnapshot = true;
		}
		if (advanceCount != 1)
		{
			localToOriginFrame += advanceCount - 1;
			if (localToOriginFrame < 0)
			{
				localToOriginFrame += frameCount;
			}
			else if (localToOriginFrame >= frameCount)
			{
				localToOriginFrame -= frameCount;
			}
			originToLocalFrame = frameCount - localToOriginFrame;
			if (originToLocalFrame < 0)
			{
				originToLocalFrame += frameCount;
			}
		}
	}
}
