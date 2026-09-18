using UnityEngine;

public class CardBarHandler : MonoBehaviour
{
	private CardBar[] cardBars;

	public static CardBarHandler instance;

	private void Start()
	{
		instance = this;
		cardBars = GetComponentsInChildren<CardBar>();
	}

	public void AddCard(int teamId, CardInfo card)
	{
		cardBars[teamId].AddCard(card);
	}

	public void ResetCardBards()
	{
		for (int i = 0; i < cardBars.Length; i++)
		{
			cardBars[i].ClearBar();
		}
	}
}
