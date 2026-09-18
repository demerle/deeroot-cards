using System.Collections.Generic;
using UnityEngine;

namespace SoundImplementation
{
	public class SoundOnePerPlayer
	{
		private List<Transform> remoteControlPlayerTransformList = new List<Transform>();

		private List<int> remoteControlNumberOfList = new List<int>();

		private void AddTransformToList(Transform transform)
		{
			remoteControlPlayerTransformList.Add(transform);
			remoteControlNumberOfList.Add(0);
		}

		public int GetNumberOf(Transform transform)
		{
			for (int i = 0; i < remoteControlPlayerTransformList.Count; i++)
			{
				if (remoteControlPlayerTransformList[i] == transform)
				{
					return remoteControlNumberOfList[i];
				}
			}
			AddTransformToList(transform);
			return GetNumberOf(transform);
		}

		public void AddNumberOf(Transform transform, int toAdd)
		{
			for (int i = 0; i < remoteControlPlayerTransformList.Count; i++)
			{
				if (remoteControlPlayerTransformList[i] == transform)
				{
					remoteControlNumberOfList[i] += toAdd;
					return;
				}
			}
			AddTransformToList(transform);
			AddNumberOf(transform, toAdd);
		}
	}
}
