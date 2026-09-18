using System.Collections;
using System.Collections.Generic;

namespace Photon.Compression.Internal;

public delegate SerializationFlags UnpackListDelegate<T>(ref List<T> value, BitArray mask, byte[] buffer, ref int bitposition, int frameId, SerializationFlags writeFlags) where T : struct;
