using UnityEngine;

public class Gravity : MonoBehaviour
{
	public float gravityForce;

	public float exponent = 1f;

	private PlayerVelocity rig;

	private CharacterData data;

	private void Start()
	{
		data = GetComponent<CharacterData>();
		rig = GetComponent<PlayerVelocity>();
	}

	private void FixedUpdate()
	{
		float num = data.sinceGrounded;
		if (data.sinceWallGrab < num)
		{
			num = data.sinceWallGrab;
		}
		if (num > 0f)
		{
			rig.AddForce(Vector3.down * TimeHandler.timeScale * Mathf.Pow(num, exponent) * gravityForce * rig.mass, ForceMode2D.Force);
		}
		else
		{
			rig.AddForce(Vector3.down * TimeHandler.timeScale * num * gravityForce * rig.mass, ForceMode2D.Force);
		}
	}
}
