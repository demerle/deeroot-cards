namespace Photon.Compression.Internal;

public delegate SerializationFlags UnpackDelegate<T>(ref T value, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags);
