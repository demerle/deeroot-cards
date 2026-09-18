using UnityEngine;

namespace emotitron.Utilities.Networking
{
	public class AutoDestroyWrongNetLib : MonoBehaviour
	{
		public enum NetLib
		{
			UNET = 0,
			PUN = 1,
			PUN2 = 2,
			PUNAndPUN2 = 3
		}

		[SerializeField]
		public NetLib netLib;

		private void Awake()
		{
			if ((netLib & NetLib.PUN2) == 0)
			{
				Object.Destroy(base.gameObject);
			}
		}
	}
}
