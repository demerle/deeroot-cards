using UnityEngine;
using UnityEngine.SceneManagement;

namespace Photon.Pun.Simple;

[DisallowMultipleComponent]
public class AutoDestroyUnspawned : MonoBehaviour
{
	public bool onlyIfPrefab = true;

	public bool hasPrefabParent;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	public static void DestroyUnspawned()
	{
		AutoDestroyUnspawned[] array = Object.FindObjectsOfType<AutoDestroyUnspawned>();
		for (int num = array.Length - 1; num >= 0; num--)
		{
			AutoDestroyUnspawned autoDestroyUnspawned = array[num];
			if ((!autoDestroyUnspawned.onlyIfPrefab || autoDestroyUnspawned.hasPrefabParent) && autoDestroyUnspawned.gameObject.scene == SceneManager.GetActiveScene())
			{
				Object.Destroy(autoDestroyUnspawned.gameObject);
			}
		}
	}
}
