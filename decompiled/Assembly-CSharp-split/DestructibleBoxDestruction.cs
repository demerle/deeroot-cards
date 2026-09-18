using System;
using Sonigon;
using UnityEngine;

public class DestructibleBoxDestruction : MonoBehaviour
{
	public bool soundPlayDestruction;

	public SoundEvent soundBoxDestruction;

	private void Start()
	{
		DamagableEvent componentInParent = GetComponentInParent<DamagableEvent>();
		componentInParent.DieAction = (Action<Vector2>)Delegate.Combine(componentInParent.DieAction, new Action<Vector2>(Die));
	}

	private void Die(Vector2 dmg)
	{
		if (soundPlayDestruction)
		{
			SoundManager.Instance.PlayAtPosition(soundBoxDestruction, SoundManager.Instance.GetTransform(), base.transform);
		}
		Rigidbody2D[] componentsInChildren = GetComponentsInChildren<Rigidbody2D>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (base.transform != componentsInChildren[i].transform)
			{
				componentsInChildren[i].transform.SetParent(base.transform.root);
				componentsInChildren[i].transform.gameObject.SetActive(value: true);
				componentsInChildren[i].AddForce(dmg * UnityEngine.Random.Range(0f, 1f) * 500f, ForceMode2D.Impulse);
				componentsInChildren[i].AddTorque(UnityEngine.Random.Range(-1f, 1f) * 1000f, ForceMode2D.Impulse);
				componentsInChildren[i].GetComponent<RemoveAfterSeconds>().seconds = UnityEngine.Random.Range(0f, 0.5f);
				componentsInChildren[i].GetComponentInChildren<GetColor>().Start();
				componentsInChildren[i].GetComponentInChildren<ColorBlink>().timeAmount *= UnityEngine.Random.Range(0.5f, 2f);
				componentsInChildren[i].GetComponentInChildren<ColorBlink>().DoBlink();
				componentsInChildren[i].gameObject.layer = 18;
			}
		}
	}
}
