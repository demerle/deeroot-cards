using UnityEngine;

public class Looper : MonoBehaviour
{
	private float sinceLoop = 1f;

	private Camera mainCam;

	private TrailRenderer trail;

	private RayCastTrail rayTrail;

	private int loops = 3;

	private void Awake()
	{
		mainCam = MainCam.instance.transform.GetComponent<Camera>();
		trail = base.transform.root.GetComponentInChildren<TrailRenderer>();
		rayTrail = GetComponentInParent<RayCastTrail>();
	}

	private void Update()
	{
		if (loops <= 0 && sinceLoop > 0.3f)
		{
			Object.Destroy(this);
		}
		Vector3 vector = mainCam.WorldToScreenPoint(base.transform.position);
		vector.x /= Screen.width;
		vector.y /= Screen.height;
		vector = new Vector3(Mathf.Clamp(vector.x, 0f, 1f), Mathf.Clamp(vector.y, 0f, 1f), vector.z);
		if ((vector.x == 0f || vector.x == 1f || vector.y == 1f || vector.y == 0f) && sinceLoop > 0.1f)
		{
			if (vector.x == 0f)
			{
				vector.x = 1f;
			}
			else if (vector.x == 1f)
			{
				vector.x = 0f;
			}
			if (vector.y == 0f)
			{
				vector.y = 1f;
			}
			else if (vector.y == 1f)
			{
				vector.y = 0f;
			}
			vector.x *= Screen.width;
			vector.y *= Screen.height;
			base.transform.root.position = mainCam.ScreenToWorldPoint(vector);
			rayTrail.MoveRay();
			for (int i = 0; i < trail.positionCount; i++)
			{
				trail.SetPosition(i, base.transform.position);
			}
			sinceLoop = 0f;
			loops--;
		}
		else
		{
			sinceLoop += TimeHandler.deltaTime;
		}
	}
}
