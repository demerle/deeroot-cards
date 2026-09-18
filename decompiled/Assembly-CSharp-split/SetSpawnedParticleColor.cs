using System;
using UnityEngine;

public class SetSpawnedParticleColor : MonoBehaviour
{
	private Color myColor;

	private void Start()
	{
		SpawnObjects component = GetComponent<SpawnObjects>();
		component.SpawnedAction = (Action<GameObject>)Delegate.Combine(component.SpawnedAction, new Action<GameObject>(Go));
		myColor = PlayerSkinBank.GetPlayerSkinColors(GetComponentInParent<SpawnedAttack>().spawner.playerID).particleEffect;
	}

	private void Go(GameObject spawned)
	{
		ParticleSystem[] componentsInChildren = spawned.GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].startColor = myColor;
		}
		LineEffect[] componentsInChildren2 = spawned.GetComponentsInChildren<LineEffect>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			componentsInChildren2[j].useColorOverTime = false;
			componentsInChildren2[j].GetComponent<LineRenderer>().startColor = myColor;
			componentsInChildren2[j].GetComponent<LineRenderer>().endColor = myColor;
		}
	}
}
