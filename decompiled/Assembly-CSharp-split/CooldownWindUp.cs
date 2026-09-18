using UnityEngine;

public class CooldownWindUp : MonoBehaviour
{
	public AnimationCurve multiplierCurve;

	private float currentMultiplier = 1f;

	private float startValue;

	private float currentValue;

	public float increasePerShot = 1f;

	private CooldownCondition cooldown;

	private void Start()
	{
		cooldown = GetComponent<CooldownCondition>();
		startValue = cooldown.cooldown;
	}

	private void Update()
	{
		currentMultiplier = multiplierCurve.Evaluate(currentValue);
		currentValue = Mathf.Clamp(currentValue, 0f, 100f);
		cooldown.cooldown = startValue / currentMultiplier;
	}

	public void Reset()
	{
		currentValue = 0f;
	}

	public void Add()
	{
		currentValue += increasePerShot / currentMultiplier;
	}
}
