using UnityEngine;

public class ResetBlock : MonoBehaviour
{
	private Block block;

	private void Start()
	{
		block = base.transform.root.GetComponent<Block>();
	}

	public void Go()
	{
		block.ResetCD(soundPlay: false);
	}
}
