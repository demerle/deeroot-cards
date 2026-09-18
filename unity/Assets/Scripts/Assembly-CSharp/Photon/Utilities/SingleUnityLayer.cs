using System;
using UnityEngine;

namespace Photon.Utilities
{
	[Serializable]
	public class SingleUnityLayer
	{
		[SerializeField]
		[HideInInspector]
		private int m_LayerIndex;

		public int LayerIndex
		{
			get
			{
				return m_LayerIndex;
			}
		}

		public int Mask
		{
			get
			{
				return 1 << m_LayerIndex;
			}
		}

		public void Set(int _layerIndex)
		{
			if (_layerIndex > 0 && _layerIndex < 32)
			{
				m_LayerIndex = _layerIndex;
			}
		}

		public static implicit operator int(SingleUnityLayer m)
		{
			return m.LayerIndex;
		}
	}
}
