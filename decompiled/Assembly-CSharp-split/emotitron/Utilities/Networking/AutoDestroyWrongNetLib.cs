using UnityEngine;

namespace emotitron.Utilities.Networking;

public class AutoDestroyWrongNetLib : MonoBehaviour
{
	public enum NetLib
	{
		UNET,
		PUN,
		PUN2,
		PUNAndPUN2
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
