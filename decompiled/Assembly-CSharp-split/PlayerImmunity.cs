using System.Collections.Generic;
using UnityEngine;

public class PlayerImmunity : MonoBehaviour
{
	private List<Immunities> immunities = new List<Immunities>();

	private void Start()
	{
	}

	private void Update()
	{
		for (int num = immunities.Count - 1; num >= 0; num--)
		{
			immunities[num].time -= TimeHandler.deltaTime;
			if (immunities[num].time <= 0f)
			{
				immunities.RemoveAt(num);
			}
		}
	}

	public bool IsImune(float time, float dmg, string name)
	{
		for (int i = 0; i < immunities.Count; i++)
		{
			if (immunities[i].name == name)
			{
				if (dmg > immunities[i].dmg)
				{
					immunities[i].dmg = dmg;
					immunities[i].name = name;
					immunities[i].time = time;
					return false;
				}
				return true;
			}
		}
		immunities.Add(new Immunities(time, dmg, name));
		return false;
	}
}
