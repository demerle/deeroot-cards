using System;
using System.Collections.Generic;
using UnityEngine;

namespace Landfall.AI
{
	[CreateAssetMenu(fileName = "WeightDataAsset", menuName = "Landfall/AI/Weight Data")]
	public class WeightDataAsset : ScriptableObject
	{
		[Serializable]
		public struct WeightData
		{
			public double[] m_weights;
		}

		public List<WeightData> m_weightDatas;

		public void Save()
		{
		}
	}
}
