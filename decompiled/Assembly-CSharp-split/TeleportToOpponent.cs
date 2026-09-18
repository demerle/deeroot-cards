using UnityEngine;

public class TeleportToOpponent : MonoBehaviour
{
	public float time = 0.5f;

	public ParticleSystem from;

	public ParticleSystem to;

	private Player target;

	private void Start()
	{
	}

	public void Go()
	{
		if (!target)
		{
			target = PlayerManager.instance.GetOtherPlayer(GetComponentInParent<Player>());
		}
		from.Play();
		base.transform.root.transform.position = target.transform.position + (target.transform.position - base.transform.position).normalized;
		to.Play();
	}
}
