using UnityEngine;

public class DoPlayerReload : MonoBehaviour
{
	private WeaponHandler wh;

	private void Start()
	{
		wh = GetComponentInParent<WeaponHandler>();
	}

	public void DoReload()
	{
		wh.DoReload();
	}
}
