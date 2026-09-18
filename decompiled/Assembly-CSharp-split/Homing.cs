using Photon.Pun;
using Sonigon;
using UnityEngine;

public class Homing : MonoBehaviour
{
	[Header("Sound")]
	public SoundEvent soundHomingFound;

	private bool soundHomingCanPlay = true;

	[Header("Settings")]
	public float amount = 1f;

	public float scalingDrag = 1f;

	public float drag = 1f;

	public float spread = 1f;

	private MoveTransform move;

	private bool isOn;

	public RotSpring rot1;

	public RotSpring rot2;

	private FlickerEvent[] flicks;

	private PhotonView view;

	private void Start()
	{
		move = GetComponentInParent<MoveTransform>();
		flicks = GetComponentsInChildren<FlickerEvent>();
		view = GetComponentInParent<PhotonView>();
		GetComponentInParent<SyncProjectile>().active = true;
	}

	private void Update()
	{
		Player closestPlayer = PlayerManager.instance.GetClosestPlayer(base.transform.position, needVision: true);
		if ((bool)closestPlayer)
		{
			Vector3 vector = closestPlayer.transform.position + base.transform.right * move.selectedSpread * Vector3.Distance(base.transform.position, closestPlayer.transform.position) * spread;
			float num = Vector3.Angle(base.transform.root.forward, vector - base.transform.position);
			if (num < 70f)
			{
				move.velocity -= move.velocity * num * TimeHandler.deltaTime * scalingDrag;
				move.velocity -= move.velocity * TimeHandler.deltaTime * drag;
				move.velocity += Vector3.ClampMagnitude(vector - base.transform.position, 1f) * TimeHandler.deltaTime * move.localForce.magnitude * 0.025f * amount;
				move.velocity.z = 0f;
				move.velocity += Vector3.up * TimeHandler.deltaTime * move.gravity * move.multiplier;
				if (!isOn)
				{
					move.simulateGravity++;
					if (soundHomingCanPlay)
					{
						soundHomingCanPlay = false;
						SoundManager.Instance.PlayAtPosition(soundHomingFound, SoundManager.Instance.GetTransform(), base.transform);
					}
				}
				isOn = true;
				for (int i = 0; i < flicks.Length; i++)
				{
					flicks[i].isOn = true;
				}
				rot1.target = 10f;
				rot2.target = -10f;
			}
			else
			{
				if (isOn)
				{
					move.simulateGravity--;
					soundHomingCanPlay = true;
				}
				isOn = false;
				for (int j = 0; j < flicks.Length; j++)
				{
					flicks[j].isOn = false;
				}
				rot1.target = 50f;
				rot2.target = -50f;
			}
		}
		else
		{
			if (isOn)
			{
				move.simulateGravity--;
				soundHomingCanPlay = true;
			}
			isOn = false;
			for (int k = 0; k < flicks.Length; k++)
			{
				flicks[k].isOn = false;
			}
			rot1.target = 50f;
			rot2.target = -50f;
		}
	}
}
