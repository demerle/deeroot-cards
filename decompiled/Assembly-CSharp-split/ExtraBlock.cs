using UnityEngine;

public class ExtraBlock : MonoBehaviour
{
	private CharacterData data;

	private void Start()
	{
		data = GetComponentInParent<CharacterData>();
	}

	public void Go()
	{
		data.block.RPCA_DoBlock(firstBlock: false, dontSetCD: true);
	}
}
