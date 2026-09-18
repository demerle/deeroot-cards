using UnityEngine;

public class KillBox : MonoBehaviour
{
	public bool alwaysOn;

	public float toggleTime = 0.3f;

	private float timeActivated = -10f;

	public bool readyToKill;

	private BoxCollider2D box;

	private void Start()
	{
		box = GetComponent<BoxCollider2D>();
	}

	private void Update()
	{
		readyToKill = timeActivated + toggleTime > Time.time;
		if (!readyToKill)
		{
			return;
		}
		for (int i = 0; i < PlayerManager.instance.players.Count; i++)
		{
			if (box.OverlapPoint(PlayerManager.instance.players[i].transform.position))
			{
				PlayerManager.instance.players[i].data.healthHandler.TakeDamage(Vector2.up * 1000f, base.transform.position);
			}
		}
	}

	public void Activate()
	{
		timeActivated = Time.time;
	}
}
