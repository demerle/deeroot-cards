using System.Collections;
using Sonigon;
using UnityEngine;

public class DeathEffect : MonoBehaviour
{
	[Header("Sounds")]
	public SoundEvent soundPhoenixActivate;

	public SoundEvent soundPhoenixChargeLoop;

	public SoundEvent soundPhoenixRespawn;

	private SoundParameterIntensity soundParameterChargeLoopIntensity = new SoundParameterIntensity(0f, UpdateMode.Continuous);

	[Header("Settings")]
	public float forceMulti = 1f;

	public float minScale = 0.9f;

	public float maxScale = 1.1f;

	public float minDrag = 0.9f;

	public float maxDrag = 1.1f;

	public float minForce = 0.9f;

	public float maxForce = 1.1f;

	public float spread = 0.5f;

	private Rigidbody2D[] rigs;

	private Color baseColor;

	private ParticleSystem[] parts;

	public ParticleSystem partToColor;

	public ParticleSystem[] partsToColor;

	private float respawnTimeCurrent;

	private float respawnTime = 2.53f;

	private void Update()
	{
	}

	public void PlayDeath(Color color, PlayerVelocity playerRig, Vector2 vel, int playerIDToRevive = -1)
	{
		if (vel.magnitude < 30f)
		{
			vel = vel.normalized * 30f;
		}
		vel *= 1f;
		parts = GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < parts.Length; i++)
		{
			ParticleSystem.MainModule main3 = parts[i].main;
			if (parts[i].name.Contains("ROT"))
			{
				parts[i].transform.rotation = Quaternion.LookRotation(vel);
			}
		}
		ParticleSystem.MainModule main = partToColor.main;
		main.startColor = PlayerSkinBank.GetPlayerSkinColors(playerRig.GetComponent<Player>().playerID).color;
		for (int j = 0; j < partsToColor.Length; j++)
		{
			ParticleSystem.MainModule main2 = partsToColor[j].main;
			main2.startColor = PlayerSkinBank.GetPlayerSkinColors(playerRig.GetComponent<Player>().playerID).color;
		}
		if (playerIDToRevive != -1)
		{
			SoundManager.Instance.Play(soundPhoenixActivate, base.transform);
			SoundManager.Instance.Play(soundPhoenixChargeLoop, base.transform, soundParameterChargeLoopIntensity);
			StartCoroutine(RespawnPlayer(playerIDToRevive));
		}
	}

	private IEnumerator RespawnPlayer(int playerIDToRevive = -1)
	{
		while (respawnTimeCurrent < respawnTime)
		{
			soundParameterChargeLoopIntensity.intensity = respawnTimeCurrent / respawnTime;
			respawnTimeCurrent += 0.1f;
			yield return new WaitForSeconds(0.1f);
		}
		SoundManager.Instance.Play(soundPhoenixRespawn, base.transform);
		SoundManager.Instance.Stop(soundPhoenixChargeLoop, base.transform);
		PlayerManager.instance.players[playerIDToRevive].data.healthHandler.Revive(false);
		PlayerManager.instance.players[playerIDToRevive].data.block.RPCA_DoBlock(true);
	}

	private IEnumerator DoEffect(Rigidbody2D rig)
	{
		yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
		rig.GetComponent<SpriteRenderer>().color = baseColor;
	}
}
