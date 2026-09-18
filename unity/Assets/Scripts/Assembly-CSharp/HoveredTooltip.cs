using UnityEngine;

public class HoveredTooltip : MonoBehaviour
{
	private CurveAnimation anim;

	private HoverEvent hoverEvent;

	private void Start()
	{
		anim = GetComponent<CurveAnimation>();
		hoverEvent = GetComponentInParent<HoverEvent>();
	}

	private void Update()
	{
		if (!anim.IsPlaying())
		{
			if ((hoverEvent.isHovered || hoverEvent.isSelected) && anim.currentState != 0)
			{
				anim.PlayIn();
			}
			if (!hoverEvent.isHovered && !hoverEvent.isSelected && anim.currentState != CurveAnimationUse.Out)
			{
				anim.PlayOut();
			}
		}
	}

	private void OnEnable()
	{
		if ((bool)anim)
		{
			anim.currentState = CurveAnimationUse.Out;
			anim.transform.localScale = Vector3.zero;
		}
	}

	private void OnDisable()
	{
		if ((bool)anim)
		{
			anim.currentState = CurveAnimationUse.Out;
			anim.transform.localScale = Vector3.zero;
		}
	}
}
