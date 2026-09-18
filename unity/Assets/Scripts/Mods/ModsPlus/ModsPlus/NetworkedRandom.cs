using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnboundLib;
using UnboundLib.Networking;
using Photon.Pun;
using Random = UnityEngine.Random;

namespace ModsPlus
{
    public static class NetworkedRandom
    {
        private static float GenerateFloat() => Random.value;
        public static event Action<float, object[]> FloatGeneratedCallback;
        public static readonly NetworkedRandomType<float> FloatGenerator = new NetworkedRandomType<float>(GenerateFloat, () => FloatGeneratedCallback);

        private static Vector3 GenerateRandomVector() => Random.insideUnitSphere;
        public static event Action<Vector3, object[]> VectorGeneratedCallback;
        public static readonly NetworkedRandomType<Vector3> VectorGenerator = new NetworkedRandomType<Vector3>(GenerateRandomVector, () => VectorGeneratedCallback);
    }

    public sealed class NetworkedRandomType<T>
    {
        internal static Func<Action<T, object[]>> callbacksProvider;
        internal static Func<T> generator;

        internal NetworkedRandomType(Func<T> generator, Func<Action<T, object[]>> callbacksProvider)
        {
            NetworkedRandomType<T>.callbacksProvider = callbacksProvider;
            NetworkedRandomType<T>.generator = generator;
        }

        public void Generate(params object[] args)
        {
            NetworkingManager.RPC(typeof(NetworkedRandomType<T>), nameof(RPC_GenerateValue), new[] { args });
        }

        [UnboundRPC]
        private static void RPC_GenerateValue(object[] args)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            NetworkingManager.RPC(typeof(NetworkedRandomType<T>), nameof(RPC_ReceiveValue), args, generator.Invoke());
        }

        [UnboundRPC]
        private static void RPC_ReceiveValue(object[] args, T value)
        {
            callbacksProvider?.Invoke()?.Invoke(value, args);
        }
    }
}