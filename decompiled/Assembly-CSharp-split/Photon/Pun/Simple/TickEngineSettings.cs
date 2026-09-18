using Photon.Utilities;
using UnityEngine;

namespace Photon.Pun.Simple;

public class TickEngineSettings : SettingsScriptableObject<TickEngineSettings>
{
	public enum FrameCountEnum
	{
		FrameCount12 = 12,
		FrameCount30 = 30,
		FrameCount60 = 60,
		FrameCount120 = 120
	}

	public enum BufferCorrection
	{
		Manual,
		Auto
	}

	public enum LogInfoLevel
	{
		All,
		WarningsAndErrors,
		ErrorsOnly,
		None
	}

	[Space]
	[Tooltip("Enable/Disable for the NetMaster timing callbacks. This needs to be enabled for networking to work.")]
	public bool enableTickEngine = true;

	[Tooltip("Disable to pause sending Updates to Relay if no other Players are in the current Room. Default is false to save on data usage. Enable if your code requires updates to echo back to controller.")]
	public bool sendWhenSolo;

	[Header("Debugging")]
	[SerializeField]
	private LogInfoLevel logLevel = LogInfoLevel.WarningsAndErrors;

	[Tooltip("The size of the circular buffer.")]
	[SerializeField]
	[HideInInspector]
	private FrameCountEnum _frameCount = FrameCountEnum.FrameCount30;

	public static int frameCount;

	[SerializeField]
	[HideInInspector]
	private BufferCorrection _bufferCorrection;

	[Tooltip("Target size of the frame buffer. This is the number of frames in the buffer that is considered ideal. The lower the number the less latency, but with a greater risk of buffer underruns that lead to extrapolation/hitching.")]
	[SerializeField]
	[HideInInspector]
	private int _targetBufferSize = 2;

	public static int targetBufferSize;

	[Tooltip("Buffer sizes above this value wll be considered to be excessive, and will trigger multiple frames being processed to shrink the buffer.")]
	[SerializeField]
	[HideInInspector]
	private int _maxBufferSize = 3;

	public static int maxBufferSize;

	[Tooltip("Buffer sizes below this value will trigger the frames to hold for extra ticks in order to grow the buffer.")]
	[SerializeField]
	[HideInInspector]
	private int _minBufferSize = 1;

	public static int minBufferSize;

	[Tooltip("The number of ticks a buffer will be allowed to be below the the Min Buffer Size before starting to correct. This value prevents overreaction to network hiccups and allows for a few ticks before applying harsh corrections. Ideally this value will be larger than Ticks Before Shrink.")]
	[SerializeField]
	[HideInInspector]
	private int _ticksBeforeGrow = 8;

	public static int ticksBeforeGrow;

	[Tooltip("The number of ticks a buffer will be allowed to exceed Max Buffer Size before starting to correct. This value prevents overreaction to network hiccups and allows for a few ticks before applying harsh corrections. Ideally this value will be smaller than Ticks Before Grow.")]
	[SerializeField]
	[HideInInspector]
	private int _ticksBeforeShrink = 5;

	public static int ticksBeforeShrink;

	[Tooltip("States are sent post PhysX/FixedUpdate. Setting this to a value greater than one reduces these sends by only sending every X fixed tick.\n1 = Every Tick\n2 = Every Other\n3 = Every Third, etc.")]
	[SerializeField]
	[HideInInspector]
	private int _sendEveryXTick = 3;

	public static int sendEveryXTick = 3;

	[Space(4f)]
	public static int halfFrameCount;

	public static int thirdFrameCount;

	public static int quaterFrameCount;

	public static int frameCountBits;

	public static float netTickInterval;

	public static float netTickIntervalInv;

	public static float targetBufferInterval;

	private static float secondsOfBuffer;

	private static float secondsOfHalfBuffer;

	private static float bufferTargSecs;

	public static LogInfoLevel LogLevel => SettingsScriptableObject<TickEngineSettings>.Single.logLevel;

	public static int MaxKeyframes => (int)SettingsScriptableObject<TickEngineSettings>.Single._frameCount / 3;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	public static void Bootstrap()
	{
		SettingsScriptableObject<TickEngineSettings>.Single.CalculateBufferValues();
	}

	private void CalculateBufferValues()
	{
		if (_bufferCorrection == BufferCorrection.Manual)
		{
			minBufferSize = _minBufferSize;
			maxBufferSize = _maxBufferSize;
		}
		else
		{
			minBufferSize = _minBufferSize;
			maxBufferSize = _maxBufferSize;
		}
		frameCount = (int)_frameCount;
		halfFrameCount = frameCount / 2;
		thirdFrameCount = frameCount / 3;
		quaterFrameCount = frameCount / 4;
		frameCountBits = frameCount.GetBitsForMaxValue();
		netTickInterval = Time.fixedDeltaTime * (float)_sendEveryXTick;
		netTickIntervalInv = 1f / (Time.fixedDeltaTime * (float)_sendEveryXTick);
		targetBufferInterval = Time.fixedDeltaTime * (float)_sendEveryXTick * (float)_targetBufferSize;
		sendEveryXTick = _sendEveryXTick;
		targetBufferSize = _targetBufferSize;
		ticksBeforeGrow = _ticksBeforeGrow;
		ticksBeforeShrink = _ticksBeforeShrink;
		secondsOfBuffer = Time.fixedDeltaTime * (float)_sendEveryXTick * (float)frameCount;
		secondsOfHalfBuffer = secondsOfBuffer * 0.5f;
		bufferTargSecs = netTickInterval * (float)_targetBufferSize;
	}
}
