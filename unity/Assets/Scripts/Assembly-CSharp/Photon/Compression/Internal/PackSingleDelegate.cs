namespace Photon.Compression.Internal
{
	public delegate SerializationFlags PackSingleDelegate(ref float value, float preValue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
}
