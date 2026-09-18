using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities.GUIUtilities;

namespace Photon.Pun.Simple
{
	public class AutoZoom : MonoBehaviour
	{
		public static List<Transform> watched = new List<Transform>();

		public const float MAX_FOV = 75f;

		public const float MIN_FOV = 15f;

		[Range(0.1f, 0.5f)]
		[HideInInspector]
		public float window = 0.25f;

		[ValueType("/sec", 48f)]
		[HideInInspector]
		public float panRate = 20f;

		[ValueType("/sec", 48f)]
		[HideInInspector]
		public float zoomRate = 200f;

		private Camera cam;

		private void Awake()
		{
			cam = GetComponent<Camera>();
			if (!cam)
			{
				watched.Add(base.transform);
			}
		}

		private void OnDestroy()
		{
			if (watched.Contains(base.transform))
			{
				watched.Remove(base.transform);
			}
		}

		private void LateUpdate()
		{
			if ((bool)cam && cam.isActiveAndEnabled)
			{
				Bounds bounds = default(Bounds);
				for (int i = 0; i < watched.Count; i++)
				{
					Vector2 vector = cam.WorldToViewportPoint(watched[i].position);
					bounds.Encapsulate(vector + new Vector2(-0.5f, -0.5f));
				}
				if (watched.Count > 0)
				{
					cam.transform.Rotate(new Vector3(0f, 1f, 0f), bounds.center.x * Time.deltaTime * panRate);
					float num = bounds.extents.x - window;
					float num2 = bounds.extents.y - window;
					float num3 = ((num > num2) ? num : num2);
					float value = cam.fieldOfView + num3 * Time.deltaTime * zoomRate;
					cam.fieldOfView = Mathf.Clamp(value, 15f, 75f);
				}
			}
		}
	}
}
