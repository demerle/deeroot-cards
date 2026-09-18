using System;
using UnityEngine;

public class GunLevel : MonoBehaviour
{
	public Gun copyTo;

	private Gun copyFrom;

	private void Start()
	{
		copyFrom = GetComponent<Gun>();
		AttackLevel componentInParent = GetComponentInParent<AttackLevel>();
		componentInParent.LevelUpAction = (Action<int>)Delegate.Combine(componentInParent.LevelUpAction, new Action<int>(BuffGun));
	}

	public void BuffGun(int level)
	{
		ApplyCardStats.CopyGunStats(copyFrom, copyTo);
	}
}
