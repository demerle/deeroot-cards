using Photon.Pun;
using UnityEngine;

public class MoveSequence : MonoBehaviour
{
	private int targetID;

	public Vector2[] positions;

	public float drag = 1f;

	public float spring = 1f;

	public float cap = 1f;

	public float threshold = 1f;

	public float timeAtPos;

	private float counter;

	private Vector2 startPos;

	private Vector2 velocity;

	private Rigidbody2D rig;

	private Map map;

	private string myKey;

	private void Start()
	{
		base.gameObject.layer = 17;
		startPos = base.transform.localPosition;
		rig = GetComponent<Rigidbody2D>();
		map = GetComponentInParent<Map>();
		myKey = "MapObect " + GetComponentInParent<Map>().levelID + " " + base.transform.GetSiblingIndex();
		MapManager.instance.GetComponent<ChildRPC>().childRPCsInt.Add(myKey, RPCA_SetTargetID);
	}

	private void OnDestroy()
	{
		if ((bool)MapManager.instance)
		{
			MapManager.instance.GetComponent<ChildRPC>().childRPCsInt.Remove(myKey);
		}
	}

	private void OnDrawGizmos()
	{
		for (int i = 0; i < positions.Length; i++)
		{
			Gizmos.DrawSphere((Vector2)base.transform.position + positions[i], 0.25f + (float)i * 0.15f);
		}
	}

	private void Update()
	{
		if (MapTransition.isTransitioning || !map.hasEntered)
		{
			return;
		}
		Vector2 vector = positions[targetID] + startPos;
		Vector2 vector2 = vector - (Vector2)base.transform.position;
		vector2 = Vector3.ClampMagnitude(vector2, cap);
		if ((bool)rig)
		{
			rig.gravityScale = 0f;
			rig.AddForce(vector2 * spring * CappedDeltaTime.time * rig.mass);
		}
		else
		{
			velocity += vector2 * spring * CappedDeltaTime.time;
			velocity -= velocity * drag * CappedDeltaTime.time;
			base.transform.position += (Vector3)velocity * TimeHandler.deltaTime;
		}
		if (!PhotonNetwork.IsMasterClient || !(Vector2.Distance(base.transform.position, vector) < threshold))
		{
			return;
		}
		counter += TimeHandler.deltaTime;
		if (counter > timeAtPos)
		{
			targetID++;
			if (targetID >= positions.Length)
			{
				targetID = 0;
			}
			MapManager.instance.GetComponent<ChildRPC>().CallFunction(myKey, targetID);
			counter = 0f;
		}
	}

	private void RPCA_SetTargetID(int setValue)
	{
		targetID = setValue;
	}
}
