using UnityEngine;

namespace Photon.Pun.Simple
{
	public abstract class HitscanComponent : NetComponent, IOnPreSimulate
	{
		public GameObject origin;

		public HitscanDefinition hitscanDefinition = new HitscanDefinition();

		[Tooltip("Ignore any collider hits that are nested children of the same NetObject this Hitscan is on.")]
		public bool ignoreSelf = true;

		public bool visualize;

		protected bool triggerQueued;

		public override void OnAwake()
		{
			base.OnAwake();
			if (!origin)
			{
				origin = base.gameObject;
			}
		}

		public virtual void OnPreSimulate(int frameId, int subFrameId)
		{
			if (triggerQueued)
			{
				int nearestIndex = -1;
				RaycastHit[] rayhits;
				Collider[] hits;
				int hitcount = hitscanDefinition.GenericHitscanNonAlloc(base.transform, out rayhits, out hits, ref nearestIndex);
				if (visualize)
				{
					hitscanDefinition.VisualizeHitscan(base.transform);
				}
				triggerQueued = false;
				ProcessHits(hits, hitcount);
			}
		}

		public virtual void ProcessHits(Collider[] hits, int hitcount)
		{
			for (int i = 0; i < hitcount; i++)
			{
				Collider collider = hits[i];
				if (ignoreSelf)
				{
					NetObject parentComponent = collider.transform.GetParentComponent<NetObject>();
					if (parentComponent == netObj)
					{
						Debug.Log("Ignoring self " + base.name + " hit: " + (collider ? collider.name : "null") + " hitnetobj: " + (parentComponent ? parentComponent.name : "null"));
						continue;
					}
				}
				if (ProcessHit(collider))
				{
					break;
				}
			}
		}

		public abstract bool ProcessHit(Collider hit);
	}
}
