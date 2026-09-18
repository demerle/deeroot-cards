using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
	public int ID;

	public int TEAMID;

	public Vector3 localStartPos;

	private void Awake()
	{
		localStartPos = base.transform.localPosition;
	}
}
