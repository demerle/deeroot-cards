using UnityEngine;
using UnityEngine.UI;

public class RoundCounter : MonoBehaviour
{
	public int p1Rounds;

	public int p2Rounds;

	public int p1Points;

	public int p2Points;

	public Transform p1Parent;

	public Transform p2Parent;

	public Color offColor;

	public Populate p1Populate;

	public Populate p2Populate;

	private int roundsNeeded;

	private void Start()
	{
		p1Parent.gameObject.SetActive(true);
		p2Parent.gameObject.SetActive(true);
	}

	public void UpdateRounds(int r1, int r2)
	{
		p1Rounds = r1;
		p2Rounds = r2;
		ReDraw();
	}

	private void Clear()
	{
		for (int i = 0; i < p1Parent.childCount; i++)
		{
			if (p1Populate.transform.GetChild(i).gameObject.activeSelf)
			{
				Object.Destroy(p1Populate.transform.GetChild(i).gameObject);
			}
		}
		for (int j = 0; j < p2Parent.childCount; j++)
		{
			if (p2Populate.transform.GetChild(j).gameObject.activeSelf)
			{
				Object.Destroy(p2Populate.transform.GetChild(j).gameObject);
			}
		}
	}

	private void Populate()
	{
		p1Populate.times = roundsNeeded;
		p1Populate.DoPopulate();
		p2Populate.times = roundsNeeded;
		p2Populate.DoPopulate();
	}

	public void UpdatePoints(int p1, int p2)
	{
		p1Points = p1;
		p2Points = p2;
		ReDraw();
	}

	private void ReDraw()
	{
		for (int i = 0; i < p1Parent.childCount; i++)
		{
			if (p1Rounds + p1Points > i)
			{
				p1Parent.GetChild(i).GetComponentInChildren<Image>().color = PlayerSkinBank.GetPlayerSkinColors(0).winText;
				if (p1Rounds > i)
				{
					p1Parent.GetChild(i).localScale = Vector3.one;
				}
			}
			else
			{
				p1Parent.GetChild(i).GetComponentInChildren<Image>().color = offColor;
				p1Parent.GetChild(i).localScale = Vector3.one * 0.3f;
			}
		}
		for (int j = 0; j < p2Parent.childCount; j++)
		{
			if (p2Rounds + p2Points > j)
			{
				p2Parent.GetChild(j).GetComponentInChildren<Image>().color = PlayerSkinBank.GetPlayerSkinColors(1).winText;
				if (p2Rounds > j)
				{
					p2Parent.GetChild(j).localScale = Vector3.one;
				}
			}
			else
			{
				p2Parent.GetChild(j).GetComponentInChildren<Image>().color = offColor;
				p2Parent.GetChild(j).localScale = Vector3.one * 0.3f;
			}
		}
	}

	internal Vector3 GetPointPos(int teamID)
	{
		if (teamID == 0)
		{
			return p1Parent.GetChild(p1Rounds).transform.position;
		}
		return p2Parent.GetChild(p2Rounds).transform.position;
	}

	internal void SetNumberOfRounds(int roundsToWinGame)
	{
		roundsNeeded = roundsToWinGame;
		Clear();
		Populate();
		ReDraw();
	}
}
