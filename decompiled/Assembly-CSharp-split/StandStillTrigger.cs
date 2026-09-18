using UnityEngine;

public class StandStillTrigger : MonoBehaviour
{
	public float sinceStandStill;

	private CharacterData data;

	private void Start()
	{
		data = base.transform.GetComponentInParent<CharacterData>();
	}

	private void Update()
	{
		if (data.isGrounded && data.input.direction.magnitude < 0.1f)
		{
			sinceStandStill += TimeHandler.deltaTime;
		}
		else
		{
			sinceStandStill = 0f;
		}
	}
}
