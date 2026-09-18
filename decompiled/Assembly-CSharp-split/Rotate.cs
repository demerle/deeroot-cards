using UnityEngine;

public class Rotate : MonoBehaviour
{
	public float speed;

	private void Update()
	{
		base.transform.Rotate(Vector3.forward * speed * TimeHandler.deltaTime, Space.World);
	}
}
