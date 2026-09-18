using System;
using UnityEngine;

public class LevelScale : MonoBehaviour
{
	public bool onLevelUp = true;

	public bool onStart;

	private AttackLevel level;

	private Vector3 startScale;

	private bool inited;

	private void Init()
	{
		if (!inited)
		{
			inited = true;
			startScale = base.transform.localScale;
		}
	}

	private void Start()
	{
		Init();
		level = GetComponent<AttackLevel>();
		if (onStart)
		{
			base.transform.localScale *= level.LevelScale();
		}
		if (onLevelUp)
		{
			AttackLevel attackLevel = level;
			attackLevel.LevelUpAction = (Action<int>)Delegate.Combine(attackLevel.LevelUpAction, new Action<int>(LevelUp));
		}
	}

	public void LevelUp(int lvl)
	{
		Init();
		base.transform.localScale = startScale * level.LevelScale();
	}
}
