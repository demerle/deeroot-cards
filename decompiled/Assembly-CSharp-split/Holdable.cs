using UnityEngine;

public class Holdable : MonoBehaviour
{
	public Rigidbody2D rig;

	public CharacterData holder;

	private void Awake()
	{
		rig = GetComponent<Rigidbody2D>();
	}

	private void Start()
	{
	}

	public void SetTeamColors(PlayerSkin teamColor, Player player)
	{
		SetTeamColor.TeamColorThis(base.gameObject, teamColor);
	}
}
