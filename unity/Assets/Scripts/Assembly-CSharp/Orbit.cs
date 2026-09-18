using System;
using UnityEngine;

public class Orbit : MonoBehaviour
{
	public GameObject[] objectsPerLevel;

	private void Start()
	{
		AttackLevel component = GetComponent<AttackLevel>();
		component.LevelUpAction = (Action<int>)Delegate.Combine(component.LevelUpAction, new Action<int>(UpdateLevel));
	}

	private void UpdateLevel(int newLevel)
	{
		for (int i = 0; i < objectsPerLevel.Length; i++)
		{
			if (i <= newLevel - 1)
			{
				objectsPerLevel[i].SetActive(true);
			}
		}
	}
}
