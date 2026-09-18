using UnityEngine;

public class BlinkStep : MonoBehaviour
{
	public float interval = 0.29f;

	private CharacterData data;

	private float counter;

	private void Start()
	{
		data = GetComponentInParent<CharacterData>();
	}

	private void Update()
	{
		if (data.view.IsMine)
		{
			counter += TimeHandler.deltaTime;
			if (counter > interval)
			{
				counter = 0f;
				data.block.CallDoBlock(true, true);
			}
		}
	}
}
