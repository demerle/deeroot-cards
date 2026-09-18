using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities;

namespace Photon.Pun.Simple
{
	public class TeleportMarker : MonoBehaviour
	{
		public MarkerNameType markerType;

		public static Dictionary<int, List<TeleportMarker>> lookup = new Dictionary<int, List<TeleportMarker>>();

		public static Dictionary<int, int> nexts = new Dictionary<int, int>();

		private void OnEnable()
		{
			int hash = markerType.hash;
			if (!lookup.ContainsKey(hash))
			{
				lookup.Add(hash, new List<TeleportMarker>());
			}
			lookup[hash].Add(this);
		}

		private void OnDisable()
		{
			int hash = markerType.hash;
			if (lookup.ContainsKey(hash) && lookup[hash].Contains(this))
			{
				lookup[hash].Remove(this);
			}
		}

		public static TeleportMarker GetRandomMarker(int hash, float seed)
		{
			if (!lookup.ContainsKey(hash))
			{
				return null;
			}
			List<TeleportMarker> list = lookup[hash];
			int index = Random.Range(0, list.Count);
			return list[index];
		}

		public static TeleportMarker GetNextMarker(int hash)
		{
			if (!lookup.ContainsKey(hash))
			{
				return null;
			}
			List<TeleportMarker> list = lookup[hash];
			int num = nexts[hash];
			num++;
			if (num >= nexts.Count)
			{
				num = 0;
			}
			nexts[hash] = num;
			return list[num];
		}
	}
}
