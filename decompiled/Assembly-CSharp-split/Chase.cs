using UnityEngine;
using UnityEngine.Events;

public class Chase : MonoBehaviour
{
	public UnityEvent turnOnEvent;

	public UnityEvent turnOffEvent;

	public UnityEvent switchTargetEvent;

	private Player player;

	private LineEffect lineEffect;

	private bool isOn;

	private Player currentTarget;

	private void Start()
	{
		lineEffect = GetComponentInChildren<LineEffect>(includeInactive: true);
		player = GetComponentInParent<Player>();
	}

	private void Update()
	{
		Player player = PlayerManager.instance.GetClosestPlayerInTeam(base.transform.position, PlayerManager.instance.GetOtherTeam(this.player.teamID), needVision: true);
		if ((bool)player && (Vector2.Angle(player.transform.position - base.transform.position, this.player.data.input.direction) > 70f || this.player.data.input.direction == Vector3.zero))
		{
			player = null;
		}
		if ((bool)player)
		{
			if (currentTarget != this.player)
			{
				currentTarget = this.player;
				switchTargetEvent.Invoke();
				lineEffect.Play(base.transform, player.transform);
			}
			if (!isOn)
			{
				isOn = true;
				turnOnEvent.Invoke();
			}
		}
		else
		{
			if (isOn)
			{
				isOn = false;
				turnOffEvent.Invoke();
			}
			if (lineEffect.isPlaying)
			{
				lineEffect.Stop();
				lineEffect.gameObject.SetActive(value: false);
			}
			currentTarget = null;
		}
	}
}
