using Photon.Utilities;
using UnityEngine;
using emotitron;

namespace Photon.Compression.Internal
{
	public static class Pack_TestPackObject
	{
		public const int LOCAL_FIELDS = 2;

		public const int TOTAL_FIELDS = 2;

		public static PackObjectDatabase.PackObjectInfo packObjInfo;

		private static PackDelegate<float> rotationPacker;

		private static UnpackDelegate<float> rotationUnpacker;

		private static PackDelegate<int> intorobotoPacker;

		private static UnpackDelegate<int> intorobotoUnpacker;

		public static bool initialized;

		public static bool isInitializing;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void Initialize()
		{
			if (!initialized)
			{
				isInitializing = true;
				int num = 0;
				PackObjectAttribute packObjectAttribute = typeof(TestPackObject).GetCustomAttributes(typeof(PackObjectAttribute), false)[0] as PackObjectAttribute;
				DefaultKeyRate defaultKeyRate = packObjectAttribute.defaultKeyRate;
				FastBitMask128 defaultReadyMask = new FastBitMask128(2);
				int num2 = 0;
				SyncHalfFloatAttribute syncHalfFloatAttribute = typeof(TestPackObject).GetField("rotation").GetCustomAttributes(typeof(SyncVarBaseAttribute), false)[0] as SyncHalfFloatAttribute;
				rotationPacker = ((IPackSingle)syncHalfFloatAttribute).Pack;
				rotationUnpacker = ((IPackSingle)syncHalfFloatAttribute).Unpack;
				syncHalfFloatAttribute.Initialize(typeof(float));
				if (syncHalfFloatAttribute.keyRate == KeyRate.UseDefault)
				{
					syncHalfFloatAttribute.keyRate = (KeyRate)defaultKeyRate;
				}
				if (syncHalfFloatAttribute.syncAs == SyncAs.Auto)
				{
					syncHalfFloatAttribute.syncAs = packObjectAttribute.syncAs;
				}
				if (syncHalfFloatAttribute.syncAs == SyncAs.Auto)
				{
					syncHalfFloatAttribute.syncAs = SyncAs.State;
				}
				if (syncHalfFloatAttribute.syncAs == SyncAs.Trigger)
				{
					defaultReadyMask[num2] = true;
				}
				num += 16;
				num2++;
				SyncRangedIntAttribute syncRangedIntAttribute = typeof(TestPackObject).GetField("intoroboto").GetCustomAttributes(typeof(SyncVarBaseAttribute), false)[0] as SyncRangedIntAttribute;
				intorobotoPacker = ((IPackInt32)syncRangedIntAttribute).Pack;
				intorobotoUnpacker = ((IPackInt32)syncRangedIntAttribute).Unpack;
				syncRangedIntAttribute.Initialize(typeof(int));
				if (syncRangedIntAttribute.keyRate == KeyRate.UseDefault)
				{
					syncRangedIntAttribute.keyRate = (KeyRate)defaultKeyRate;
				}
				if (syncRangedIntAttribute.syncAs == SyncAs.Auto)
				{
					syncRangedIntAttribute.syncAs = packObjectAttribute.syncAs;
				}
				if (syncRangedIntAttribute.syncAs == SyncAs.Auto)
				{
					syncRangedIntAttribute.syncAs = SyncAs.State;
				}
				if (syncRangedIntAttribute.syncAs == SyncAs.Trigger)
				{
					defaultReadyMask[num2] = true;
				}
				num += 2;
				num2++;
				packObjInfo = new PackObjectDatabase.PackObjectInfo(defaultReadyMask, Pack, Pack, Unpack, num, PackFrame_TestPackObject.Factory, PackFrame_TestPackObject.Factory, PackFrame_TestPackObject.Apply, PackFrame_TestPackObject.Capture, PackFrame_TestPackObject.Copy, PackFrame_TestPackObject.SnapshotCallback, PackFrame_TestPackObject.Interpolate, PackFrame_TestPackObject.Interpolate, 2);
				PackObjectDatabase.packObjInfoLookup.Add(typeof(TestPackObject), packObjInfo);
				isInitializing = false;
				initialized = true;
			}
		}

		public static SerializationFlags Pack(ref object obj, PackFrame prevFrame, ref FastBitMask128 mask, ref int maskOffset, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			TestPackObject testPackObject = obj as TestPackObject;
			PackFrame_TestPackObject packFrame_TestPackObject = prevFrame as PackFrame_TestPackObject;
			SerializationFlags serializationFlags = rotationPacker(ref testPackObject.rotation, packFrame_TestPackObject.rotation, buffer, ref bitposition, frameId, writeFlags);
			mask[maskOffset] = serializationFlags != SerializationFlags.None;
			SerializationFlags num = SerializationFlags.None | serializationFlags;
			maskOffset++;
			SerializationFlags serializationFlags2 = intorobotoPacker(ref testPackObject.intoroboto, packFrame_TestPackObject.intoroboto, buffer, ref bitposition, frameId, writeFlags);
			mask[maskOffset] = serializationFlags2 != SerializationFlags.None;
			SerializationFlags result = num | serializationFlags2;
			maskOffset++;
			return result;
		}

		public static SerializationFlags Pack(ref TestPackObject packable, PackFrame prevFrame, ref FastBitMask128 mask, ref int maskOffset, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			PackFrame_TestPackObject packFrame_TestPackObject = prevFrame as PackFrame_TestPackObject;
			SerializationFlags serializationFlags = rotationPacker(ref packable.rotation, packFrame_TestPackObject.rotation, buffer, ref bitposition, frameId, writeFlags);
			mask[maskOffset] = serializationFlags != SerializationFlags.None;
			SerializationFlags num = SerializationFlags.None | serializationFlags;
			maskOffset++;
			SerializationFlags serializationFlags2 = intorobotoPacker(ref packable.intoroboto, packFrame_TestPackObject.intoroboto, buffer, ref bitposition, frameId, writeFlags);
			mask[maskOffset] = serializationFlags2 != SerializationFlags.None;
			SerializationFlags result = num | serializationFlags2;
			maskOffset++;
			return result;
		}

		public static SerializationFlags Pack(PackFrame obj, PackFrame prevFrame, ref FastBitMask128 mask, ref int maskOffset, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			PackFrame_TestPackObject packFrame_TestPackObject = obj as PackFrame_TestPackObject;
			PackFrame_TestPackObject packFrame_TestPackObject2 = prevFrame as PackFrame_TestPackObject;
			SerializationFlags serializationFlags = rotationPacker(ref packFrame_TestPackObject.rotation, packFrame_TestPackObject2.rotation, buffer, ref bitposition, frameId, writeFlags);
			mask[maskOffset] = serializationFlags != SerializationFlags.None;
			SerializationFlags num = SerializationFlags.None | serializationFlags;
			maskOffset++;
			SerializationFlags serializationFlags2 = intorobotoPacker(ref packFrame_TestPackObject.intoroboto, packFrame_TestPackObject2.intoroboto, buffer, ref bitposition, frameId, writeFlags);
			mask[maskOffset] = serializationFlags2 != SerializationFlags.None;
			SerializationFlags result = num | serializationFlags2;
			maskOffset++;
			return result;
		}

		public static SerializationFlags Unpack(PackFrame obj, ref FastBitMask128 mask, ref FastBitMask128 isCompleteMask, ref int maskOffset, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			PackFrame_TestPackObject packFrame_TestPackObject = obj as PackFrame_TestPackObject;
			SerializationFlags serializationFlags = SerializationFlags.None;
			if (mask[maskOffset])
			{
				SerializationFlags serializationFlags2 = rotationUnpacker(ref packFrame_TestPackObject.rotation, buffer, ref bitposition, frameId, writeFlags);
				isCompleteMask[maskOffset] = (serializationFlags2 & SerializationFlags.IsComplete) != 0;
				mask[maskOffset] = serializationFlags2 != SerializationFlags.None;
				serializationFlags |= serializationFlags2;
			}
			maskOffset++;
			if (mask[maskOffset])
			{
				SerializationFlags serializationFlags3 = intorobotoUnpacker(ref packFrame_TestPackObject.intoroboto, buffer, ref bitposition, frameId, writeFlags);
				isCompleteMask[maskOffset] = (serializationFlags3 & SerializationFlags.IsComplete) != 0;
				mask[maskOffset] = serializationFlags3 != SerializationFlags.None;
				serializationFlags |= serializationFlags3;
			}
			maskOffset++;
			return serializationFlags;
		}

		public static SerializationFlags Pack(ref PackFrame_TestPackObject packable, PackFrame prevFrame, ref FastBitMask128 mask, ref int maskOffset, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			PackFrame_TestPackObject packFrame_TestPackObject = prevFrame as PackFrame_TestPackObject;
			SerializationFlags serializationFlags = rotationPacker(ref packable.rotation, packFrame_TestPackObject.rotation, buffer, ref bitposition, frameId, writeFlags);
			mask[maskOffset] = serializationFlags != SerializationFlags.None;
			SerializationFlags num = SerializationFlags.None | serializationFlags;
			maskOffset++;
			SerializationFlags serializationFlags2 = intorobotoPacker(ref packable.intoroboto, packFrame_TestPackObject.intoroboto, buffer, ref bitposition, frameId, writeFlags);
			mask[maskOffset] = serializationFlags2 != SerializationFlags.None;
			SerializationFlags result = num | serializationFlags2;
			maskOffset++;
			return result;
		}

		public static SerializationFlags Unpack(ref PackFrame_TestPackObject packable, ref FastBitMask128 mask, ref FastBitMask128 isCompleteMask, ref int maskOffset, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			SerializationFlags serializationFlags = SerializationFlags.None;
			if (mask[maskOffset])
			{
				SerializationFlags serializationFlags2 = rotationUnpacker(ref packable.rotation, buffer, ref bitposition, frameId, writeFlags);
				isCompleteMask[maskOffset] = (serializationFlags2 & SerializationFlags.IsComplete) != 0;
				mask[maskOffset] = serializationFlags2 != SerializationFlags.None;
				serializationFlags |= serializationFlags2;
			}
			maskOffset++;
			if (mask[maskOffset])
			{
				SerializationFlags serializationFlags3 = intorobotoUnpacker(ref packable.intoroboto, buffer, ref bitposition, frameId, writeFlags);
				isCompleteMask[maskOffset] = (serializationFlags3 & SerializationFlags.IsComplete) != 0;
				mask[maskOffset] = serializationFlags3 != SerializationFlags.None;
				serializationFlags |= serializationFlags3;
			}
			maskOffset++;
			return serializationFlags;
		}
	}
}
