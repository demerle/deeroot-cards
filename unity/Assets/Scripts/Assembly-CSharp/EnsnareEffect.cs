using Sirenix.OdinInspector;
using Sonigon;
using UnityEngine;

public class EnsnareEffect : MonoBehaviour
{
	[Header("Sounds")]
	public SoundEvent soundEnsnare;

	public SoundEvent soundEnsnareBreak;

	public bool soundEnsnareJumpChange;

	public float range = 1f;

	private float seconds = 1f;

	public float force = 2000f;

	public float breakDistance = 2f;

	public float drag = 0.98f;

	private CharacterData target;

	private Vector2 startPos;

	public bool startPosIsMyPos;

	private float scale = 1f;

	private LineEffect line;

	[FoldoutGroup("SNAP", 0)]
	public float snapCapLineTime = 0.5f;

	[FoldoutGroup("SNAP", 0)]
	public float snapCapTimeTime = 0.5f;

	private bool done;

	private bool doneDone;

	private void Start()
	{
		scale *= base.transform.localScale.x;
		range *= (1f + base.transform.localScale.x) * 0.5f;
		Player closestPlayer = PlayerManager.instance.GetClosestPlayer(base.transform.position, true);
		if ((bool)closestPlayer && Vector3.Distance(base.transform.position, closestPlayer.transform.position) < range)
		{
			target = closestPlayer.data;
			startPos = target.transform.position;
			if (startPosIsMyPos)
			{
				startPos = base.transform.position;
			}
			float bezierOffset = 1f;
			if (startPosIsMyPos)
			{
				bezierOffset = 0f;
			}
			line = GetComponentInChildren<LineEffect>(true);
			line.Play(base.transform, closestPlayer.transform, bezierOffset);
			if (soundEnsnare != null)
			{
				SoundManager.Instance.Play(soundEnsnare, target.transform);
			}
			if (soundEnsnareJumpChange)
			{
				target.playerSounds.AddEnsnareEffect(this);
			}
		}
	}

	private void FixedUpdate()
	{
		if (!target || !(line.currentWidth > 0f))
		{
			return;
		}
		target.sinceGrounded = 0f;
		target.playerVel.velocity *= Mathf.Pow(drag, scale);
		target.playerVel.AddForce(Vector2.ClampMagnitude(startPos - (Vector2)target.transform.position, 60f) * scale * force, ForceMode2D.Force);
		if (!done && Vector2.Distance(startPos, target.transform.position) > breakDistance)
		{
			done = true;
			line.counter = Mathf.Clamp(line.counter, snapCapLineTime, float.PositiveInfinity);
			if (soundEnsnareJumpChange)
			{
				target.playerSounds.RemoveEnsnareEffect(this);
			}
			if (soundEnsnareBreak != null)
			{
				SoundManager.Instance.Play(soundEnsnareBreak, target.transform);
			}
		}
	}
}
