using UnityEngine;

namespace Photon.Pun.Simple
{
	public class AutoLayerByAuthority : MonoBehaviour
	{
		public int authorityLayer = 8;

		public int nonAuthorityLayer = 9;

		public int projectileLayer = 10;

		public bool applyToChildren = true;

		private void Awake()
		{
			Physics.IgnoreLayerCollision(authorityLayer, projectileLayer);
			if (applyToChildren)
			{
				SetChildrenLayer(base.transform, projectileLayer);
			}
			else
			{
				base.gameObject.layer = projectileLayer;
			}
		}

		public void OnChangeAuthority(bool IsMine, bool serverIsActive)
		{
			Debug.Log("Auth change " + base.name);
			int layer = (IsMine ? authorityLayer : nonAuthorityLayer);
			if (applyToChildren)
			{
				SetChildrenLayer(base.transform, layer);
			}
			else
			{
				base.gameObject.layer = layer;
			}
		}

		public void SetChildrenLayer(Transform t, int layer)
		{
			t.gameObject.layer = layer;
			for (int i = 0; i < t.childCount; i++)
			{
				if (!t.GetChild(i).GetComponent<AutoLayerByAuthority>())
				{
					SetChildrenLayer(t.GetChild(i), layer);
				}
			}
		}
	}
}
