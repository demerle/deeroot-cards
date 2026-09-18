using Photon.Utilities;
using emotitron;

namespace Photon.Compression.Internal
{
	public class PackFrame_TestPackObject : PackFrame
	{
		public float rotation;

		public int intoroboto;

		public static void Interpolate(PackFrame start, PackFrame end, PackFrame trg, float time, ref FastBitMask128 readyMask, ref int maskOffset)
		{
			PackFrame_TestPackObject packFrame_TestPackObject = start as PackFrame_TestPackObject;
			PackFrame_TestPackObject packFrame_TestPackObject2 = end as PackFrame_TestPackObject;
			PackFrame_TestPackObject packFrame_TestPackObject3 = trg as PackFrame_TestPackObject;
			FastBitMask128 fastBitMask = end.mask;
			if (fastBitMask[maskOffset])
			{
				packFrame_TestPackObject3.rotation = (packFrame_TestPackObject2.rotation - packFrame_TestPackObject.rotation) * time + packFrame_TestPackObject.rotation;
			}
			maskOffset++;
			if (fastBitMask[maskOffset])
			{
				packFrame_TestPackObject3.intoroboto = (int)((float)(packFrame_TestPackObject2.intoroboto - packFrame_TestPackObject.intoroboto) * time) + packFrame_TestPackObject.intoroboto;
			}
			maskOffset++;
		}

		public static void Interpolate(PackFrame start, PackFrame end, object trg, float time, ref FastBitMask128 readyMask, ref int maskOffset)
		{
			PackFrame_TestPackObject packFrame_TestPackObject = start as PackFrame_TestPackObject;
			PackFrame_TestPackObject packFrame_TestPackObject2 = end as PackFrame_TestPackObject;
			TestPackObject testPackObject = trg as TestPackObject;
			FastBitMask128 fastBitMask = end.mask;
			if (readyMask[maskOffset] && fastBitMask[maskOffset])
			{
				testPackObject.rotation = (packFrame_TestPackObject2.rotation - packFrame_TestPackObject.rotation) * time + packFrame_TestPackObject.rotation;
			}
			maskOffset++;
			maskOffset++;
		}

		public static void SnapshotCallback(PackFrame snapframe, PackFrame targframe, object trg, ref FastBitMask128 readyMask, ref int maskOffset)
		{
			PackFrame_TestPackObject packFrame_TestPackObject = snapframe as PackFrame_TestPackObject;
			PackFrame_TestPackObject packFrame_TestPackObject2 = targframe as PackFrame_TestPackObject;
			TestPackObject testPackObject = trg as TestPackObject;
			FastBitMask128 fastBitMask = snapframe.mask;
			FastBitMask128 fastBitMask2 = targframe.mask;
			if (readyMask[maskOffset])
			{
				float num = (fastBitMask[maskOffset] ? packFrame_TestPackObject.rotation : testPackObject.rotation);
				float targ = (fastBitMask2[maskOffset] ? packFrame_TestPackObject2.rotation : num);
				testPackObject.SnapshotHook(num, targ);
			}
			maskOffset++;
			maskOffset++;
		}

		public static void Capture(object src, PackFrame trg)
		{
			TestPackObject testPackObject = src as TestPackObject;
			PackFrame_TestPackObject obj = trg as PackFrame_TestPackObject;
			obj.rotation = testPackObject.rotation;
			obj.intoroboto = testPackObject.intoroboto;
		}

		public static void Apply(PackFrame src, object trg, ref FastBitMask128 mask, ref int maskOffset)
		{
			PackFrame_TestPackObject packFrame_TestPackObject = src as PackFrame_TestPackObject;
			TestPackObject testPackObject = trg as TestPackObject;
			if (mask[maskOffset])
			{
				float oldrot = testPackObject.rotation;
				testPackObject.rotation = packFrame_TestPackObject.rotation;
				testPackObject.RotationHook(packFrame_TestPackObject.rotation, oldrot);
			}
			maskOffset++;
			if (mask[maskOffset])
			{
				testPackObject.intoroboto = packFrame_TestPackObject.intoroboto;
			}
			maskOffset++;
		}

		public static void Copy(PackFrame src, PackFrame trg)
		{
			PackFrame_TestPackObject packFrame_TestPackObject = src as PackFrame_TestPackObject;
			PackFrame_TestPackObject obj = trg as PackFrame_TestPackObject;
			obj.rotation = packFrame_TestPackObject.rotation;
			obj.intoroboto = packFrame_TestPackObject.intoroboto;
		}

		public static PackFrame Factory()
		{
			return new PackFrame_TestPackObject
			{
				mask = new FastBitMask128(2),
				isCompleteMask = new FastBitMask128(2)
			};
		}

		public static PackFrame[] Factory(object trg, int count)
		{
			PackFrame_TestPackObject[] array = new PackFrame_TestPackObject[count];
			for (int i = 0; i < count; i++)
			{
				PackFrame_TestPackObject packFrame_TestPackObject = new PackFrame_TestPackObject
				{
					mask = new FastBitMask128(2),
					isCompleteMask = new FastBitMask128(2)
				};
				array[i] = packFrame_TestPackObject;
			}
			return array;
		}
	}
}
