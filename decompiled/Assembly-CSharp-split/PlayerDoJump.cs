using UnityEngine;

public class PlayerDoJump : MonoBehaviour
{
	private PlayerJump jump;

	public float multiplier = 0.25f;

	private void Start()
	{
		jump = GetComponentInParent<PlayerJump>();
	}

	public void DoJump()
	{
		jump.Jump(forceJump: true, multiplier);
	}
}
