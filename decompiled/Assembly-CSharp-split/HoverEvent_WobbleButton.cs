using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HoverEvent_WobbleButton : MonoBehaviour
{
	public float min = 1f;

	public float max = 1.1f;

	public float spring = 1f;

	public float drag = 1f;

	public float force = -0.1f;

	private ScaleShake sh;

	private void Awake()
	{
		HoverEvent hoverEvent = base.gameObject.AddComponent<HoverEvent>();
		sh = base.gameObject.AddComponent<ScaleShake>();
		Button component = GetComponent<Button>();
		sh.spring = spring;
		sh.drag = drag;
		sh.multiplier = force;
		sh.high = max;
		sh.low = min;
		sh.useTimeScale = false;
		sh.SetTarget(min);
		UnityEvent unityEvent = new UnityEvent();
		unityEvent.AddListener(sh.SetHigh);
		hoverEvent.enterEvent = unityEvent;
		UnityEvent unityEvent2 = new UnityEvent();
		unityEvent2.AddListener(sh.SetLow);
		hoverEvent.exitEvent = unityEvent2;
		component.onClick.AddListener(sh.AddForce);
	}

	private void OnDisable()
	{
		base.transform.localScale = Vector3.one * min;
		sh.SetTarget(min);
	}
}
