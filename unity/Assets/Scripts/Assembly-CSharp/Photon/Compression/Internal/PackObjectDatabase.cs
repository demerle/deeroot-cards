using System;
using System.Collections.Generic;
using Photon.Utilities;

namespace Photon.Compression.Internal
{
	public static class PackObjectDatabase
	{
		public delegate SerializationFlags PackStructDelegate(IntPtr obj, PackFrame prevFrame, ref FastBitMask128 mask, ref int maskOffset, byte[] buffer, ref int bitposition, int frameId, SerializationFlags forceKeyframe);

		public delegate SerializationFlags PackObjDelegate(ref object obj, PackFrame prevFrame, ref FastBitMask128 mask, ref int maskOffset, byte[] buffer, ref int bitposition, int frameId, SerializationFlags forceKeyframe);

		public delegate SerializationFlags PackFrameDelegate(PackFrame obj, PackFrame prevFrame, ref FastBitMask128 mask, ref int maskOffset, byte[] buffer, ref int bitposition, int frameId, SerializationFlags forceKeyframe);

		public delegate SerializationFlags UnpackFrameDelegate(PackFrame obj, ref FastBitMask128 hasContentMask, ref FastBitMask128 isCompleteMask, ref int maskOffset, byte[] buffer, ref int bitposition, int frameId, SerializationFlags forceKeyframe);

		public delegate void PackCopyFrameToObjectDelegate(PackFrame src, object trg, ref FastBitMask128 mask, ref int maskOffset);

		public delegate void PackCopyFrameToStructDelegate(PackFrame src, IntPtr trg, ref FastBitMask128 mask, ref int maskOffset);

		public delegate void PackSnapshotObjectDelegate(PackFrame snap, PackFrame targ, object trg, ref FastBitMask128 readyMask, ref int maskOffset);

		public delegate void PackSnapshotStructDelegate(PackFrame snap, PackFrame targ, IntPtr trg, ref FastBitMask128 readyMask, ref int maskOffset);

		public delegate void PackInterpFrameToFrameDelegate(PackFrame start, PackFrame end, PackFrame trg, float ntime, ref FastBitMask128 readyMask, ref int maskOffset);

		public delegate void PackInterpFrameToObjectDelegate(PackFrame start, PackFrame end, object trg, float ntime, ref FastBitMask128 readyMask, ref int maskOffset);

		public delegate void PackInterpFrameToStructDelegate(PackFrame start, PackFrame end, IntPtr trg, float ntime, ref FastBitMask128 readyMask, ref int maskOffset);

		public class PackObjectInfo
		{
			public readonly Type packFrameType;

			public readonly int maxBits;

			public readonly int maxBytes;

			public readonly FastBitMask128 defaultReadyMask;

			public readonly PackObjDelegate PackObjToBuffer;

			public readonly PackStructDelegate PackStructToBuffer;

			public readonly PackFrameDelegate PackFrameToBuffer;

			public readonly UnpackFrameDelegate UnpackFrameFromBuffer;

			public Func<PackFrame> FactoryFrame;

			public Func<object, int, PackFrame[]> FactoryFramesObj;

			public Func<IntPtr, int, PackFrame[]> FactoryFramesStruct;

			public PackCopyFrameToObjectDelegate CopyFrameToObj;

			public PackCopyFrameToStructDelegate CopyFrameToStruct;

			public PackSnapshotObjectDelegate SnapObject;

			public PackSnapshotStructDelegate SnapStruct;

			public PackInterpFrameToFrameDelegate InterpFrameToFrame;

			public PackInterpFrameToObjectDelegate InterpFrameToObj;

			public PackInterpFrameToStructDelegate InterpFrameToStruct;

			public Action<object, PackFrame> CaptureObj;

			public Action<IntPtr, PackFrame> CaptureStruct;

			public Action<PackFrame, PackFrame> CopyFrameToFrame;

			public readonly int fieldCount;

			public PackObjectInfo(FastBitMask128 defaultReadyMask, PackObjDelegate packObjToBuffer, PackFrameDelegate packFrameToBuffer, UnpackFrameDelegate unpackFrameFromBuffer, int maxBits, Func<PackFrame> factoryFrame, Func<object, int, PackFrame[]> factoryFramesObj, PackCopyFrameToObjectDelegate copyFrameToObj, Action<object, PackFrame> captureObj, Action<PackFrame, PackFrame> copyFrameToFrame, PackSnapshotObjectDelegate snapObject, PackInterpFrameToFrameDelegate interpFrameToFrame, PackInterpFrameToObjectDelegate interpFrameToObj, int fieldCount)
			{
				PackObjToBuffer = packObjToBuffer;
				this.defaultReadyMask = defaultReadyMask;
				PackFrameToBuffer = packFrameToBuffer;
				UnpackFrameFromBuffer = unpackFrameFromBuffer;
				this.maxBits = maxBits;
				maxBytes = maxBits + 7 >> 3;
				FactoryFrame = factoryFrame;
				FactoryFramesObj = factoryFramesObj;
				CopyFrameToObj = copyFrameToObj;
				CaptureObj = captureObj;
				CopyFrameToFrame = copyFrameToFrame;
				SnapObject = snapObject;
				InterpFrameToFrame = interpFrameToFrame;
				InterpFrameToObj = interpFrameToObj;
				this.fieldCount = fieldCount;
			}

			public PackObjectInfo(FastBitMask128 defaultReadyMask, PackStructDelegate packStructToBuffer, PackFrameDelegate packFrameToBuffer, UnpackFrameDelegate unpackFrameFromBuffer, int maxBits, Func<PackFrame> factoryFrame, Func<IntPtr, int, PackFrame[]> factoryFramesStruct, PackCopyFrameToStructDelegate copyFrameToStruct, Action<IntPtr, PackFrame> captureStruct, Action<PackFrame, PackFrame> copyFrameToFrame, PackSnapshotStructDelegate snapStruct, PackInterpFrameToFrameDelegate interpFrameToFrame, PackInterpFrameToStructDelegate interpFrameToStruct, int fieldCount)
			{
				this.defaultReadyMask = defaultReadyMask;
				PackStructToBuffer = packStructToBuffer;
				PackFrameToBuffer = packFrameToBuffer;
				UnpackFrameFromBuffer = unpackFrameFromBuffer;
				this.maxBits = maxBits;
				maxBytes = maxBits + 7 >> 3;
				FactoryFrame = factoryFrame;
				FactoryFramesStruct = factoryFramesStruct;
				CopyFrameToStruct = copyFrameToStruct;
				CaptureStruct = captureStruct;
				CopyFrameToFrame = copyFrameToFrame;
				SnapStruct = snapStruct;
				InterpFrameToFrame = interpFrameToFrame;
				InterpFrameToStruct = interpFrameToStruct;
				this.fieldCount = fieldCount;
			}
		}

		public static Dictionary<Type, PackObjectInfo> packObjInfoLookup = new Dictionary<Type, PackObjectInfo>();

		public static PackObjectInfo GetPackObjectInfo(Type type)
		{
			PackObjectInfo value;
			if (packObjInfoLookup.TryGetValue(type, out value))
			{
				return value;
			}
			Type type2 = Type.GetType("Pack_" + type.Name);
			if (type2 != null)
			{
				Debug.LogError("BRUTE FORCE Pack_" + type.Name + ". This shouldn't happen.");
				type2.GetMethod("Initialize").Invoke(null, null);
			}
			if (packObjInfoLookup.TryGetValue(type, out value))
			{
				return value;
			}
			return null;
		}
	}
}
