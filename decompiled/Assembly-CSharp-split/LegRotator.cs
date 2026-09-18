using UnityEngine;

public class LegRotator : MonoBehaviour
{
	private PlayerVelocity rig;

	private void Start()
	{
		rig = GetComponentInParent<PlayerVelocity>();
	}

	private void Update()
	{
		if ((bool)rig)
		{
			if (rig.velocity.x < 0f)
			{
				base.transform.localEulerAngles = Vector3.Lerp(base.transform.localEulerAngles, new Vector3(0f, 0f, 0f), TimeHandler.deltaTime * 15f * Mathf.Clamp(Mathf.Abs(rig.velocity.x), 0f, 1f));
			}
			else
			{
				base.transform.localEulerAngles = Vector3.Lerp(base.transform.localEulerAngles, new Vector3(0f, 180f, 0f), TimeHandler.deltaTime * 15f * Mathf.Clamp(Mathf.Abs(rig.velocity.x), 0f, 1f));
			}
		}
	}
}
