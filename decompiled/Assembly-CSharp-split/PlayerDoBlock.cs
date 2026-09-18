using Photon.Pun;
using UnityEngine;

public class PlayerDoBlock : MonoBehaviour
{
	private CharacterData data;

	private SyncPlayerMovement sync;

	private void Start()
	{
		data = GetComponentInParent<CharacterData>();
		sync = GetComponentInParent<SyncPlayerMovement>();
	}

	public void DoBlock()
	{
		if (data.view.IsMine)
		{
			sync.SendBlock(BlockTrigger.BlockTriggerType.Default, firstBlock: true, dontSetCD: true);
		}
	}
}
