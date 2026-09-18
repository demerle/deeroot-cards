using System.Collections.Generic;

namespace Photon.Compression.Internal;

public delegate SerializationFlags PackListDelegate<T>(ref List<T> value, List<T> prevvalue, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags) where T : struct;
