using UnityEngine;

public class SpawnMinion : MonoBehaviour
{
	public GameObject card;

	public GameObject minionAI;

	public GameObject minion;

	private CharacterData data;

	private AttackLevel level;

	private void Start()
	{
		data = GetComponentInParent<CharacterData>();
		level = GetComponentInParent<AttackLevel>();
	}

	public void Go()
	{
		for (int i = 0; i < level.attackLevel; i++)
		{
			GameObject gameObject = Object.Instantiate(minion, base.transform.position + Vector3.up * (((float)i + 1f) * 0.5f), base.transform.rotation);
			Object.Instantiate(minionAI, gameObject.transform.position, gameObject.transform.rotation, gameObject.transform);
			CharacterData component = gameObject.GetComponent<CharacterData>();
			component.SetAI(data.player);
			component.player.playerID = data.player.playerID;
			component.isPlaying = true;
			card.GetComponent<ApplyCardStats>().Pick(component.player.teamID, true);
			gameObject.GetComponentInChildren<PlayerSkinHandler>().ToggleSimpleSkin(true);
			component.healthHandler.DestroyOnDeath = true;
		}
	}
}
