using Sonigon;
using UnityEngine;

public class DealDamageToPlayer : MonoBehaviour
{
	public enum TargetPlayer
	{
		Own = 0,
		Other = 1
	}

	[Header("Sounds")]
	public SoundEvent soundDamage;

	[Header("Settings")]
	public float damage = 25f;

	public bool lethal = true;

	public TargetPlayer targetPlayer;

	private CharacterData data;

	private Player target;

	private void Start()
	{
		data = GetComponentInParent<CharacterData>();
	}

	public void Go()
	{
		if (!target)
		{
			target = data.player;
			if (targetPlayer == TargetPlayer.Other)
			{
				target = PlayerManager.instance.GetOtherPlayer(target);
			}
		}
		if (soundDamage != null && target != null && target.data != null && target.data.isPlaying && !target.data.dead && !target.data.block.IsBlocking())
		{
			SoundManager.Instance.Play(soundDamage, target.transform);
		}
		target.data.healthHandler.TakeDamage(damage * Vector2.up, base.transform.position, null, data.player, lethal);
	}
}
