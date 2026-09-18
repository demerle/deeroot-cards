using UnityEngine;

public class DynamicParticles : MonoBehaviour
{
	public static DynamicParticles instance;

	public DynamicParticleSet[] bulletHit;

	private int spawnsThisFrame;

	private void Start()
	{
		instance = this;
	}

	private void Update()
	{
		spawnsThisFrame = 0;
	}

	public void PlayBulletHit(float damage, Transform spawnerTransform, HitInfo hit, Color projectielColor)
	{
		if ((float)spawnsThisFrame > 5f)
		{
			return;
		}
		spawnsThisFrame++;
		int num = 0;
		for (int i = 1; i < bulletHit.Length && !(bulletHit[i].dmg > damage); i++)
		{
			num = i;
		}
		GameObject[] array = ObjectsToSpawn.SpawnObject(base.transform, hit, bulletHit[num].objectsToSpawn, null, null);
		if (!(projectielColor != Color.black))
		{
			return;
		}
		for (int j = 0; j < array.Length; j++)
		{
			for (int k = 0; k < array[j].transform.childCount; k++)
			{
				ChangeColor componentInChildren = array[j].transform.GetChild(k).GetComponentInChildren<ChangeColor>();
				if ((bool)componentInChildren)
				{
					componentInChildren.GetComponent<ParticleSystemRenderer>().material.color = projectielColor;
				}
			}
		}
	}
}
