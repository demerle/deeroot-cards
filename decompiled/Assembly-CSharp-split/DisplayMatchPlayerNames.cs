using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class DisplayMatchPlayerNames : MonoBehaviour
{
	public TextMeshProUGUI local;

	public TextMeshProUGUI other;

	public void ShowNames()
	{
		List<Photon.Realtime.Player> list = PhotonNetwork.CurrentRoom.Players.Select((KeyValuePair<int, Photon.Realtime.Player> p) => p.Value).ToList();
		bool flag = false;
		flag = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(NetworkConnectionHandler.TWITCH_ROOM_AUDIENCE_RATING_KEY);
		for (int num = 0; num < list.Count; num++)
		{
			string text = (flag ? (" (" + list[num].CustomProperties[NetworkConnectionHandler.TWITCH_PLAYER_SCORE_KEY].ToString() + ")") : string.Empty);
			if (num == 0)
			{
				local.text = list[num].NickName + text;
			}
			else
			{
				other.text = list[num].NickName + text;
			}
		}
	}
}
