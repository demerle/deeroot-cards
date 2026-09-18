namespace Photon.Compression.Internal
{
	public class SyncRangedAttribute : SyncVarBaseAttribute, IPackSingle
	{
		private LiteFloatCrusher crusher = new LiteFloatCrusher();

		public SyncRangedAttribute(LiteFloatCompressType compression, float min, float max, bool accurateCenter)
		{
			LiteFloatCrusher.Recalculate(compression, min, max, accurateCenter, crusher);
		}

		public SerializationFlags Pack(ref float value, float preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			uint num = (uint)crusher.Encode(value);
			if (!IsForced(frameId, writeFlags) && num == (uint)crusher.Encode(preValue))
			{
				return SerializationFlags.None;
			}
			crusher.WriteCValue(num, buffer, ref bitposition);
			return SerializationFlags.IsComplete;
		}

		public SerializationFlags Unpack(ref float value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags)
		{
			value = crusher.ReadValue(buffer, ref bitposition);
			return SerializationFlags.IsComplete;
		}
	}
}
