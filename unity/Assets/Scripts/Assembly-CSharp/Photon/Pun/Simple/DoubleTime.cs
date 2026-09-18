using UnityEngine;

namespace Photon.Pun.Simple
{
	public static class DoubleTime
	{
		public static double time;

		public static double prevUpdateTime;

		public static double prevFixedTime;

		public static double deltaTime;

		public static double timeSinceFixed;

		public static double fixedTime;

		public static double fixedDeltaTime;

		public static float normTimeSinceFixed;

		public static double updateTime;

		public static float mixedDeltaTime;

		public static bool isInFixed;

		private static bool isFirstUpdatePostFixed;

		public static void SnapFixed()
		{
			prevFixedTime = fixedTime;
			fixedDeltaTime = Time.fixedDeltaTime;
			if (fixedTime == 0.0)
			{
				fixedTime = Time.fixedTime;
				time = Time.time;
			}
			else
			{
				fixedTime += fixedDeltaTime;
				mixedDeltaTime = (float)(fixedTime - time);
				time = fixedTime;
			}
			isInFixed = true;
			isFirstUpdatePostFixed = true;
		}

		public static void SnapUpdate()
		{
			prevUpdateTime = updateTime;
			if (updateTime == 0.0)
			{
				updateTime = Time.time;
				fixedTime = Time.fixedTime;
				time = fixedTime;
			}
			else
			{
				updateTime += Time.deltaTime;
			}
			timeSinceFixed = updateTime - fixedTime;
			deltaTime = updateTime - prevUpdateTime;
			normTimeSinceFixed = (float)(timeSinceFixed / (double)Time.fixedDeltaTime);
			mixedDeltaTime = (isFirstUpdatePostFixed ? ((float)(updateTime - time)) : Time.deltaTime);
			time = updateTime;
			isFirstUpdatePostFixed = false;
			isInFixed = false;
		}
	}
}
