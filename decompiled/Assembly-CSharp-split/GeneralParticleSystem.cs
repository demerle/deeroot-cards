using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GeneralParticleSystem : MonoBehaviour
{
	public GameObject particleObject;

	public bool useTimeScale;

	private ObjectPool particlePool;

	[FoldoutGroup("Spawn settings", 0)]
	public float randomXPos;

	[FoldoutGroup("Spawn settings", 0)]
	public float randomYPos;

	[FoldoutGroup("Spawn settings", 0)]
	public float rate = 10f;

	[FoldoutGroup("Spawn settings", 0)]
	public bool playOnAwake = true;

	[FoldoutGroup("Spawn settings", 0)]
	public bool playOnEnablee;

	[FoldoutGroup("Spawn settings", 0)]
	public bool loop;

	[FoldoutGroup("Spawn settings", 0)]
	public float duration = 2f;

	[FoldoutGroup("Spawn settings", 0)]
	public AnimationCurve sizeMultiplierOverTime = AnimationCurve.Linear(0f, 1f, 1f, 1f);

	public AnimationCurve emissionMultiplierOverTime = AnimationCurve.Linear(0f, 1f, 1f, 1f);

	public ObjectParticle particleSettings;

	[FoldoutGroup("Global settings", 0)]
	public float simulationSpeed = 1f;

	[HideInInspector]
	public float simulationSpeedMultiplier = 1f;

	[FoldoutGroup("Global settings", 0)]
	public float saturationMultiplier = 1f;

	[FoldoutGroup("Events", 0)]
	public UnityEvent startEvent;

	[FoldoutGroup("Events", 0)]
	public float startEventDelay;

	[FoldoutGroup("Events", 0)]
	public UnityEvent endEvent;

	[FoldoutGroup("Events", 0)]
	public float endEventDelay;

	private float sizeOverTimeAnimationCurveLength;

	private float sizeMultiplierOverTimeAnimationCurveLength;

	private float alphaOverTimeAnimationCurveLength;

	private float emissionOverTimeAnimationCurveLength;

	private bool inited;

	private Coroutine emissionLoop;

	private float lastEmissionTime;

	private bool isPlaying;

	private void OnEnable()
	{
		isPlaying = false;
		if (playOnEnablee)
		{
			Play();
		}
	}

	private void Start()
	{
		if (playOnAwake)
		{
			Play();
		}
		Mask component = base.transform.parent.GetComponent<Mask>();
		if ((bool)component)
		{
			component.showMaskGraphic = false;
		}
	}

	private void Init()
	{
		if (!inited)
		{
			inited = true;
			if (particleSettings.sizeOverTime.keys.Length > 1)
			{
				sizeOverTimeAnimationCurveLength = particleSettings.sizeOverTime.keys[particleSettings.sizeOverTime.keys.Length - 1].time - particleSettings.sizeOverTime.keys[0].time;
			}
			if (particleSettings.alphaOverTime.keys.Length > 1)
			{
				alphaOverTimeAnimationCurveLength = particleSettings.alphaOverTime.keys[particleSettings.alphaOverTime.keys.Length - 1].time - particleSettings.alphaOverTime.keys[0].time;
			}
			if (sizeMultiplierOverTime.keys.Length > 1)
			{
				sizeMultiplierOverTimeAnimationCurveLength = sizeMultiplierOverTime.keys[sizeMultiplierOverTime.keys.Length - 1].time - sizeMultiplierOverTime.keys[0].time;
			}
			if (emissionMultiplierOverTime.keys.Length > 1)
			{
				emissionOverTimeAnimationCurveLength = emissionMultiplierOverTime.keys[emissionMultiplierOverTime.keys.Length - 1].time - emissionMultiplierOverTime.keys[0].time;
			}
			particleObject.SetActive(value: false);
			particlePool = new ObjectPool(particleObject, 100, base.transform);
		}
	}

	public void StartLooping()
	{
		loop = true;
		Play();
	}

	public void EndLooping()
	{
		loop = false;
		if (emissionLoop != null)
		{
			StopCoroutine(emissionLoop);
		}
	}

	[Button]
	public void Play()
	{
		Init();
		if (!isPlaying)
		{
			emissionLoop = StartCoroutine(DoPlay());
		}
	}

	private void OnDisable()
	{
		Stop();
	}

	public void Stop()
	{
		isPlaying = false;
		DisableAllParticles();
		if (emissionLoop != null)
		{
			StopCoroutine(emissionLoop);
		}
	}

	private void DisableAllParticles()
	{
		if (base.transform.childCount > 0)
		{
			for (int i = 0; i < base.transform.GetChild(0).childCount; i++)
			{
				base.transform.GetChild(0).GetChild(i).gameObject.SetActive(value: false);
			}
		}
	}

	private IEnumerator DoPlay()
	{
		isPlaying = true;
		if (startEvent != null)
		{
			if (startEventDelay != 0f)
			{
				StartCoroutine(DelayEvent(startEvent, startEventDelay));
			}
			else
			{
				startEvent.Invoke();
			}
		}
		float counter = 0f;
		while (counter < duration)
		{
			CheckIfShouldEmit(counter / duration);
			counter += (useTimeScale ? TimeHandler.deltaTime : Time.unscaledDeltaTime) * (simulationSpeed * simulationSpeedMultiplier);
			yield return null;
		}
		isPlaying = false;
		if (loop)
		{
			Play();
		}
		else if (endEvent != null)
		{
			if (endEventDelay != 0f)
			{
				StartCoroutine(DelayEvent(endEvent, endEventDelay));
			}
			else
			{
				endEvent.Invoke();
			}
		}
	}

	private void CheckIfShouldEmit(float currentAnimationTime)
	{
		if ((useTimeScale ? Time.time : Time.unscaledTime) > lastEmissionTime + 1f / rate / (simulationSpeed * simulationSpeedMultiplier) / emissionMultiplierOverTime.Evaluate(currentAnimationTime * emissionOverTimeAnimationCurveLength) * Time.timeScale)
		{
			StartCoroutine(DoPlarticleLife(currentAnimationTime));
			lastEmissionTime = Time.time;
		}
	}

	private IEnumerator DoPlarticleLife(float currentAnimationTime)
	{
		GameObject spawned = particlePool.GetObject();
		float counter = 0f;
		float t = particleSettings.lifetime;
		Vector3 startSize = spawned.transform.localScale;
		Vector3 modifiedStartSize = spawned.transform.localScale * particleSettings.size * sizeMultiplierOverTime.Evaluate(currentAnimationTime * sizeMultiplierOverTimeAnimationCurveLength);
		Image img = spawned.GetComponent<Image>();
		Color startColor = Color.magenta;
		if ((bool)img)
		{
			startColor = img.color;
		}
		if ((bool)img)
		{
			float value = Random.value;
			if (particleSettings.color != Color.magenta)
			{
				img.color = particleSettings.color;
			}
			if (particleSettings.randomColor != Color.magenta)
			{
				img.color = Color.Lerp(img.color, particleSettings.randomColor, value);
			}
			if (!particleSettings.singleRandomValueColor)
			{
				value = Random.value;
			}
			if (particleSettings.randomAddedColor != Color.black)
			{
				img.color += Color.Lerp(Color.black, particleSettings.randomAddedColor, value);
			}
			if (!particleSettings.singleRandomValueColor)
			{
				value = Random.value;
			}
			if (particleSettings.randomAddedSaturation != 0f || saturationMultiplier != 1f)
			{
				Color.RGBToHSV(img.color, out var H, out var S, out var V);
				S += value * particleSettings.randomAddedSaturation;
				S *= saturationMultiplier;
				img.color = Color.HSVToRGB(H, S, V);
			}
		}
		spawned.transform.Rotate(base.transform.forward * particleSettings.rotation);
		spawned.transform.Rotate(base.transform.forward * Random.Range(0f - particleSettings.randomRotation, particleSettings.randomRotation));
		spawned.transform.localPosition = Vector3.zero;
		spawned.transform.position += base.transform.up * Random.Range(0f - randomYPos, randomYPos);
		spawned.transform.position += base.transform.right * Random.Range(0f - randomXPos, randomXPos);
		spawned.transform.position += base.transform.forward * Random.Range(-0.1f, 0.1f);
		while (counter < t)
		{
			if (particleSettings.sizeOverTime.keys.Length > 1)
			{
				spawned.transform.localScale = modifiedStartSize * particleSettings.sizeOverTime.Evaluate(counter / t * sizeOverTimeAnimationCurveLength);
			}
			float num = particleSettings.alphaOverTime.Evaluate(counter / t * alphaOverTimeAnimationCurveLength);
			if ((bool)img && img.color.a != num)
			{
				img.color = new Color(img.color.r, img.color.g, img.color.b, num);
			}
			counter += (useTimeScale ? TimeHandler.deltaTime : Time.unscaledDeltaTime) * (simulationSpeed * simulationSpeedMultiplier);
			yield return null;
		}
		if ((bool)img)
		{
			img.color = startColor;
		}
		spawned.transform.localScale = startSize;
		particlePool.ReleaseObject(spawned);
	}

	private IEnumerator DelayEvent(UnityEvent e, float t)
	{
		yield return new WaitForSeconds(t);
		e?.Invoke();
	}
}
