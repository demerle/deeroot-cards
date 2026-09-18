using UnityEngine;

public class LowerScreenshakePerPlayer : MonoBehaviour
{
	public Screenshaker shake;

	private void Update()
	{
		if (PlayerManager.instance.players.Count > 2)
		{
			shake.shakeforce *= 0.5f;
			Object.Destroy(this);
		}
	}
}
