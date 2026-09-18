using UnityEngine;

public class ParticlePlayer : MonoBehaviour
{
	public static ParticlePlayer instance;

	private int spawnsThisFrame;

	private void Awake()
	{
		instance = this;
	}

	private void Update()
	{
		spawnsThisFrame = 0;
	}

	public void PlayEffect(string effectName, Vector3 position, Quaternion rotation, float scale = 1f, Transform followTransform = null)
	{
		if ((float)spawnsThisFrame > 5f)
		{
			return;
		}
		spawnsThisFrame++;
		Transform transform = base.transform.Find(effectName);
		if (!transform)
		{
			return;
		}
		if ((bool)followTransform)
		{
			transform = Object.Instantiate(transform.gameObject, null).transform;
		}
		transform.transform.position = position;
		transform.transform.localScale = scale * Vector3.one;
		transform.transform.rotation = rotation;
		if ((bool)followTransform)
		{
			transform.gameObject.AddComponent<FollowLocalPos>().Follow(followTransform);
		}
		ParticleSystem[] componentsInChildren = transform.GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if ((bool)followTransform)
			{
				ParticleSystem.MainModule main = componentsInChildren[i].main;
				main.simulationSpace = ParticleSystemSimulationSpace.Local;
			}
			componentsInChildren[i].Play();
		}
	}
}
