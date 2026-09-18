using System;
using UnityEngine;

public class AttackLevel : MonoBehaviour
{
	public int attackLevel = 1;

	public float levelScaleM = 0.7f;

	public Action<int> LevelUpAction;

	private void Start()
	{
		SpawnedAttack componentInParent = GetComponentInParent<SpawnedAttack>();
		if ((bool)componentInParent)
		{
			attackLevel = componentInParent.attackLevel;
		}
		bool flag = false;
		AttackLevel[] componentsInChildren = base.transform.root.GetComponentsInChildren<AttackLevel>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (!(componentsInChildren[i] == this) && componentsInChildren[i].gameObject.name == base.gameObject.name)
			{
				componentsInChildren[i].LevelUp();
				flag = true;
			}
		}
		if (flag)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public float LevelScale()
	{
		return 1f + ((float)attackLevel - 1f) * levelScaleM;
	}

	public int LevelsUp()
	{
		return attackLevel - 1;
	}

	public void LevelUp()
	{
		attackLevel++;
		if (LevelUpAction != null)
		{
			LevelUpAction(attackLevel);
		}
	}
}
