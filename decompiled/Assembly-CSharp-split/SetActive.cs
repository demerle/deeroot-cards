using UnityEngine;

public class SetActive : MonoBehaviour
{
	public void DoSetActive(bool active)
	{
		base.gameObject.SetActive(active);
	}
}
