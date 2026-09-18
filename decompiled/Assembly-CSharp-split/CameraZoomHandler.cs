using UnityEngine;

public class CameraZoomHandler : MonoBehaviour
{
	private Camera[] cameras;

	private void Start()
	{
		cameras = GetComponentsInChildren<Camera>();
	}

	private void Update()
	{
		float b = 20f;
		if (MapManager.instance.currentMap != null)
		{
			b = MapManager.instance.currentMap.Map.size;
		}
		for (int i = 0; i < cameras.Length; i++)
		{
			cameras[i].orthographicSize = Mathf.Lerp(cameras[i].orthographicSize, b, Time.unscaledDeltaTime * 5f);
		}
	}
}
