using UnityEngine;

public class SpawnStaticRemnant : MonoBehaviour
{
	public GameObject remnantSource;

	private AttackLevel level;

	private Color remnantColor;

	private void Start()
	{
		remnantColor = PlayerSkinBank.GetPlayerSkinColors(base.transform.GetComponentInParent<Player>().playerID).winText;
		level = GetComponent<AttackLevel>();
	}

	public void Go()
	{
		GameObject gameObject = Object.Instantiate(remnantSource, base.transform.position, base.transform.rotation);
		SpriteRenderer[] componentsInChildren = base.transform.root.GetComponentsInChildren<SpriteRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].transform.lossyScale.x != 0f && componentsInChildren[i].transform.lossyScale.y != 0f && componentsInChildren[i].transform.lossyScale.z != 0f && (!(componentsInChildren[i].transform.parent.name != "Art") || !(componentsInChildren[i].transform.parent.parent.name != "Face")))
			{
				Vector3 lossyScale = componentsInChildren[i].transform.lossyScale;
				GameObject gameObject2 = Object.Instantiate(componentsInChildren[i].gameObject, componentsInChildren[i].transform.position, componentsInChildren[i].transform.rotation, gameObject.transform.GetChild(0));
				gameObject2.transform.localScale = lossyScale;
				Strip(gameObject2);
				SpriteRenderer component = gameObject2.GetComponent<SpriteRenderer>();
				component.enabled = true;
				component.color = remnantColor;
				SpriteMask component2 = gameObject2.GetComponent<SpriteMask>();
				if ((bool)component2)
				{
					Object.Destroy(component2);
				}
			}
		}
		gameObject.GetComponentInChildren<ParticleSystem>().startColor = PlayerSkinBank.GetPlayerSkinColors(base.transform.GetComponentInParent<Player>().playerID).particleEffect;
		gameObject.AddComponent<SpawnedAttack>().spawner = base.transform.root.GetComponent<Player>();
		gameObject.transform.localScale *= 1f + (float)(level.attackLevel - 1) * 0.3f;
	}

	private void Strip(GameObject go)
	{
		MonoBehaviour[] componentsInChildren = go.GetComponentsInChildren<MonoBehaviour>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Object.Destroy(componentsInChildren[i]);
		}
	}
}
