using UnityEngine;

public class GifCam : MonoBehaviour
{
	public static bool isGifCam;

	private Camera[] cameras;

	private Vector3 camStartPos;

	private float cameraStartSize;

	private bool follow = true;

	private void Start()
	{
		cameras = GetComponentsInChildren<Camera>();
		if (cameras.Length <= 0) return;
		camStartPos = cameras[0].transform.position;
		cameraStartSize = cameras[0].orthographicSize;
	}

	private void Update()
	{
	}
}
