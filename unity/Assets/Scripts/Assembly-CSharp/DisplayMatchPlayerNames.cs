using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class DisplayMatchPlayerNames : MonoBehaviour
{
	[Serializable]
	[CompilerGenerated]
	private sealed class _003C_003Ec
	{
		public static readonly _003C_003Ec _003C_003E9 = new _003C_003Ec();

		public static Func<KeyValuePair<int, Photon.Realtime.Player>, Photon.Realtime.Player> _003C_003E9__2_0;

		internal Photon.Realtime.Player _003CShowNames_003Eb__2_0(KeyValuePair<int, Photon.Realtime.Player> p)
		{
			return p.Value;
		}
	}

	public TextMeshProUGUI local;

	public TextMeshProUGUI other;

	public void ShowNames()
	{
		List<Photon.Realtime.Player> list = PhotonNetwork.CurrentRoom.Players.Select(_003C_003Ec._003C_003E9__2_0 ?? (_003C_003Ec._003C_003E9__2_0 = _003C_003Ec._003C_003E9._003CShowNames_003Eb__2_0)).ToList();
		bool flag = false;
		flag = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(NetworkConnectionHandler.TWITCH_ROOM_AUDIENCE_RATING_KEY);
		for (int i = 0; i < list.Count; i++)
		{
			string text = (flag ? (" (" + list[i].CustomProperties[NetworkConnectionHandler.TWITCH_PLAYER_SCORE_KEY].ToString() + ")") : string.Empty);
			if (i == 0)
			{
				local.text = list[i].NickName + text;
			}
			else
			{
				other.text = list[i].NickName + text;
			}
		}
	}
}
