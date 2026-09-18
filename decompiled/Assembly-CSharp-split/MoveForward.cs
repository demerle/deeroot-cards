using UnityEngine;

public class MoveForward : MonoBehaviour
{
	public float amount = 3f;

	private void Start()
	{
		base.transform.position += Vector3.forward * amount;
	}
}
