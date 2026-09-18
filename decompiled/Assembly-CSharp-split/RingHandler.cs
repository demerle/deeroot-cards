using UnityEngine;

public class RingHandler : MonoBehaviour
{
	public float timeBeforeRing = 30f;

	public static RingHandler instance;

	private CodeAnimation code;

	private float counter;

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		code = GetComponent<CodeAnimation>();
	}

	private void Update()
	{
		if (GameManager.instance.isPlaying)
		{
			if (code.currentState != CodeAnimationInstance.AnimationUse.In && counter > timeBeforeRing && !code.isPlaying)
			{
				code.PlayIn();
			}
			counter += TimeHandler.deltaTime;
		}
		else
		{
			if (code.currentState != CodeAnimationInstance.AnimationUse.Out && !code.isPlaying)
			{
				code.PlayOut();
			}
			counter = 0f;
		}
	}

	public Vector2 ClosestPoint(Vector2 pos)
	{
		return pos.normalized * Radius();
	}

	public float Radius()
	{
		return base.transform.localScale.x * 95f;
	}
}
