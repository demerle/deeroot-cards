using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnVisualEffect : MonoBehaviour
{
	public Dictionary<string, GameObject> visualEffects;

	public void RPCA_SpawnVisualEffect(string effect)
	{
		Object.Instantiate(visualEffects[effect], base.transform.position, base.transform.rotation, base.transform);
	}
}
