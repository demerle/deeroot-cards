using System;

namespace Photon.Pun.Simple;

public abstract class FrameBase
{
	public int frameId;

	public FrameContents content;

	public FrameBase()
	{
	}

	public FrameBase(int frameId)
	{
		this.frameId = frameId;
	}

	public static TFrame Instantiate<TFrame>(int frameId) where TFrame : FrameBase
	{
		TFrame obj = (TFrame)Activator.CreateInstance(typeof(TFrame));
		obj.frameId = frameId;
		return obj;
	}

	public virtual void CopyFrom(FrameBase sourceFrame)
	{
		content = sourceFrame.content;
	}

	public virtual void Clear()
	{
		content = FrameContents.Empty;
	}

	public static void PopulateFrames<TFrame>(ref TFrame[] frames) where TFrame : FrameBase
	{
		int frameCount = TickEngineSettings.frameCount;
		frames = new TFrame[frameCount + 1];
		for (int i = 0; i <= frameCount; i++)
		{
			TFrame val = Instantiate<TFrame>(i);
			frames[i] = val;
		}
	}
}
