using UnityEngine;

namespace Photon.Pun.Simple;

public class SyncContactScan : SyncShootBase, IOnSnapshot, IOnAuthorityChanged
{
	public bool poke = true;

	public bool grab = true;

	public HitscanDefinition hitscanDefinition;

	[Tooltip("Render widgets that represent the shape of the hitscan when triggered.")]
	public bool visualizeHitscan = true;

	public override int ApplyOrder => 17;

	protected override bool Trigger(Frame frame, int subFrameId, float timeshift = 0f)
	{
		if ((bool)GetComponent<SyncContact>() && !photonView.IsMine)
		{
			hitscanDefinition.VisualizeHitscan(origin);
			return true;
		}
		int nearestIndex = -1;
		RaycastHit[] rayhits;
		Collider[] hits;
		int num = hitscanDefinition.GenericHitscanNonAlloc(origin, out rayhits, out hits, ref nearestIndex, visualizeHitscan);
		if (num <= 0)
		{
			return true;
		}
		for (int i = 0; i < num; i++)
		{
			IContactTrigger nestedComponentInParents = hits[i].transform.GetNestedComponentInParents<IContactTrigger, NetObject>();
			if (nestedComponentInParents != null && !(nestedComponentInParents.NetObj == contactTrigger.NetObj))
			{
				if (poke)
				{
					contactTrigger.OnContact(nestedComponentInParents, ContactType.Hitscan);
				}
				if (grab)
				{
					nestedComponentInParents.OnContact(contactTrigger, ContactType.Hitscan);
				}
			}
		}
		return true;
	}
}
