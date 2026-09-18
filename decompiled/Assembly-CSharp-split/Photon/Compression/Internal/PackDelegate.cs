namespace Photon.Compression.Internal;

public delegate SerializationFlags PackDelegate<T>(ref T value, T prevvalue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
