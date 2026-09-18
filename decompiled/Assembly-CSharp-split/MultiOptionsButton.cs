using UnityEngine;

public class MultiOptionsButton : MonoBehaviour
{
	public Resolution currentRess;

	public Optionshandler.FullScreenOption currentFull;

	public void Click()
	{
		GetComponentInParent<MultiOptions>().ClickRessButton(this);
	}
}
