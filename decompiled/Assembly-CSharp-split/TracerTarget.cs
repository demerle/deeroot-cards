using UnityEngine;

public class TracerTarget : MonoBehaviour
{
	private MoveTransform move;

	public bool hasPos;

	public float drag;

	public float spring;

	public AnimationCurve curve;

	public float cosScale = 1f;

	public float cosAmount;

	private float random;

	private float c;

	private bool done;

	private Vector3 tPos;

	private Vector3 upDir;

	private MoveTransform mTrans;

	private void Start()
	{
		move = GetComponent<MoveTransform>();
		random = Random.Range(0f, 1000f);
		SetPos(base.transform.forward * 100f, base.transform.up, null);
	}

	private void Update()
	{
		float num = 1f;
		if ((bool)mTrans)
		{
			num += mTrans.velocity.magnitude * 0.1f;
		}
		if ((bool)mTrans && !done)
		{
			tPos += base.transform.forward + base.transform.forward * 10f;
		}
		c += TimeHandler.deltaTime;
		if (hasPos)
		{
			move.velocity += cosAmount * Mathf.Cos((Time.time + random) * cosScale) * upDir * CappedDeltaTime.time / num;
			move.velocity += (tPos - base.transform.position).normalized * spring * CappedDeltaTime.time * curve.Evaluate(c) * num;
			move.velocity -= move.velocity * CappedDeltaTime.time * drag * curve.Evaluate(c);
		}
	}

	public void SetPos(Vector3 targetPos, Vector3 up, MoveTransform move)
	{
		mTrans = move;
		hasPos = true;
		tPos = targetPos;
		upDir = up;
	}
}
