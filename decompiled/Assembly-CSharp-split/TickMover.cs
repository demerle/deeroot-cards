using Photon.Pun.Simple;
using UnityEngine;

public class TickMover : MonoBehaviour, IOnPostSimulate
{
	private Vector3 rotationPerTick;

	private TextMesh tickText;

	private void Awake()
	{
		NetMasterCallbacks.RegisterCallbackInterfaces(this);
		rotationPerTick = new Vector3(0f, 0f, 360f * (Time.fixedDeltaTime * (float)TickEngineSettings.sendEveryXTick));
		tickText = GetComponentInChildren<TextMesh>();
		if (!tickText)
		{
			tickText = GetComponentInParent<TextMesh>();
		}
		if ((bool)tickText)
		{
			tickText.text = "";
		}
	}

	private void OnDestroy()
	{
		NetMasterCallbacks.RegisterCallbackInterfaces(this, register: false, delay: true);
	}

	public void OnPostSimulate(int frameId, int subFrameId, bool isNetTick)
	{
		if (isNetTick)
		{
			base.transform.eulerAngles -= rotationPerTick;
			if ((bool)tickText)
			{
				tickText.text = frameId.ToString();
			}
		}
	}
}
