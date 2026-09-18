using System.Collections;
using SoundImplementation;
using UnityEngine;
using UnityEngine.Events;

public class MapTransition : MonoBehaviour
{
	[Header("Settings")]
	public UnityEvent switchMapEvent;

	public AnimationCurve curve;

	public AnimationCurve gravityCurve;

	public static MapTransition instance;

	private const float mapPadding = 90f;

	public static bool isTransitioning;

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
	}

	public void SetStartPos(Map map)
	{
		map.transform.position = Vector3.right * 90f;
	}

	public void Enter(Map map)
	{
		MoveObject(map.gameObject, Vector3.zero);
		StartCoroutine(DelayEvent(0.1f));
		SoundPlayerStatic.Instance.PlayLevelTransitionIn();
		SoundMusicManager.Instance.PlayIngame(false);
	}

	public void Exit(Map map)
	{
		SoundPlayerStatic.Instance.PlayLevelTransitionOut();
		map.MapMoveOut();
		MoveObject(map.gameObject, Vector3.right * -90f);
		StartCoroutine(ClearObjectsAfterSeconds(1f));
	}

	private void MoveObject(GameObject target, Vector3 targetPos)
	{
		for (int i = 0; i < target.transform.childCount; i++)
		{
			Toggle(target.transform.GetChild(i).gameObject, false);
			StartCoroutine(Move(target.transform.GetChild(i).gameObject, targetPos - target.transform.position, (i == 0) ? target.GetComponent<Map>() : null));
		}
	}

	private void Toggle(GameObject obj, bool enabled)
	{
		Rigidbody2D component = obj.GetComponent<Rigidbody2D>();
		if ((bool)component)
		{
			component.simulated = enabled;
			if (enabled)
			{
				StartCoroutine(LerpDrag(component));
			}
		}
		Collider2D component2 = obj.GetComponent<Collider2D>();
		if ((bool)component2)
		{
			component2.enabled = enabled;
		}
		CodeAnimation component3 = obj.GetComponent<CodeAnimation>();
		if ((bool)component3)
		{
			component3.enabled = enabled;
		}
	}

	private IEnumerator DelayEvent(float delay)
	{
		yield return new WaitForSecondsRealtime(delay);
		switchMapEvent.Invoke();
	}

	private IEnumerator Move(GameObject target, Vector3 distance, Map targetMap = null)
	{
		isTransitioning = true;
		float maxRandomDelay = 0.25f;
		float randomDelay = Random.Range(0f, maxRandomDelay);
		yield return new WaitForSecondsRealtime(randomDelay);
		Vector3 targetStartPos = target.transform.position;
		float t = curve.keys[curve.keys.Length - 1].time;
		float c = 0f;
		while (c < t)
		{
			c += Time.unscaledDeltaTime;
			target.transform.position = targetStartPos + distance * curve.Evaluate(c);
			yield return null;
		}
		target.transform.position = targetStartPos + distance;
		Toggle(target, true);
		yield return new WaitForSecondsRealtime(maxRandomDelay - randomDelay);
		isTransitioning = false;
		if ((bool)targetMap)
		{
			targetMap.hasEntered = true;
		}
	}

	private IEnumerator LerpDrag(Rigidbody2D target)
	{
		target.gravityScale = 0f;
		float t = gravityCurve.keys[gravityCurve.keys.Length - 1].time;
		float c = 0f;
		while (c < t && (bool)target)
		{
			target.gravityScale = gravityCurve.Evaluate(c);
			c += Time.unscaledDeltaTime;
			yield return null;
		}
		if ((bool)target)
		{
			target.gravityScale = 1f;
		}
	}

	private IEnumerator ClearObjectsAfterSeconds(float time)
	{
		yield return new WaitForSecondsRealtime(time);
		ClearObjects();
	}

	public void ClearObjects()
	{
		RemoveAfterSeconds[] array = Object.FindObjectsOfType<RemoveAfterSeconds>();
		for (int i = 0; i < array.Length; i++)
		{
			Object.Destroy(array[i].gameObject);
		}
	}
}
