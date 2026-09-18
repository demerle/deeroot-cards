using System;
using System.Collections;
using UnityEngine;

public class GameCrownHandler : MonoBehaviour
{
	private float crownPos;

	public AnimationCurve transitionCurve;

	private GM_ArmsRace gm;

	private int currentCrownHolder = -1;

	private void Start()
	{
		gm = GetComponentInParent<GM_ArmsRace>();
		GM_ArmsRace gM_ArmsRace = gm;
		gM_ArmsRace.pointOverAction = (Action)Delegate.Combine(gM_ArmsRace.pointOverAction, new Action(PointOver));
	}

	public void PointOver()
	{
		int num = -1;
		int num2 = -1;
		if (gm.p1Rounds > gm.p2Rounds)
		{
			num2 = 0;
		}
		if (gm.p1Rounds < gm.p2Rounds)
		{
			num2 = 1;
		}
		if (num2 == -1)
		{
			int num3 = -1;
			if (gm.p1Points > gm.p2Points)
			{
				num3 = 0;
			}
			if (gm.p1Points < gm.p2Points)
			{
				num3 = 1;
			}
			if (num3 != -1)
			{
				num = num3;
			}
		}
		else
		{
			num = num2;
		}
		if (num != -1 && num != currentCrownHolder)
		{
			if (currentCrownHolder == -1)
			{
				currentCrownHolder = num;
				crownPos = currentCrownHolder;
				GetComponent<CurveAnimation>().PlayIn();
			}
			else
			{
				GiveCrownToPlayer(num);
			}
		}
	}

	private void LateUpdate()
	{
		if (currentCrownHolder == -1)
		{
			return;
		}
		if (PlayerManager.instance.players[currentCrownHolder].gameObject.activeInHierarchy)
		{
			if (!base.transform.GetChild(0).gameObject.activeSelf)
			{
				base.transform.GetChild(0).gameObject.SetActive(true);
			}
		}
		else if (base.transform.GetChild(0).gameObject.activeSelf)
		{
			base.transform.GetChild(0).gameObject.SetActive(false);
		}
		Vector3 position = Vector3.LerpUnclamped(PlayerManager.instance.players[0].data.GetCrownPos(), PlayerManager.instance.players[1].data.GetCrownPos(), crownPos);
		base.transform.position = position;
	}

	private void GiveCrownToPlayer(int playerID)
	{
		currentCrownHolder = playerID;
		StartCoroutine(IGiveCrownToPlayer(playerID));
	}

	private IEnumerator IGiveCrownToPlayer(int playerID)
	{
		int fromInt;
		int toInt;
		if (playerID == 0)
		{
			fromInt = 1;
			toInt = 0;
		}
		else
		{
			fromInt = 0;
			toInt = 1;
		}
		for (float i = 0f; i < transitionCurve.keys[transitionCurve.keys.Length - 1].time; i += Time.unscaledDeltaTime)
		{
			crownPos = Mathf.LerpUnclamped(fromInt, toInt, transitionCurve.Evaluate(i));
			yield return null;
		}
	}
}
