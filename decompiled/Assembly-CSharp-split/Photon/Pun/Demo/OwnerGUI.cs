using Photon.Pun.Simple;
using UnityEngine.UI;

namespace Photon.Pun.Demo;

public class OwnerGUI : NetComponent
{
	private Text text;

	public override void OnAwake()
	{
		base.OnAwake();
		text = GetComponentInChildren<Text>();
	}

	private void Update()
	{
		if ((bool)text)
		{
			PhotonView photonView = base.photonView;
			text.text = photonView.OwnerActorNr + " : " + photonView.ControllerActorNr;
		}
	}
}
