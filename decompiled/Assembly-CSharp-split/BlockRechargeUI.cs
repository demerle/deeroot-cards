using UnityEngine;
using UnityEngine.UI;

public class BlockRechargeUI : MonoBehaviour
{
	private Block block;

	private Image img;

	private void Start()
	{
		img = GetComponentInChildren<Image>();
		block = GetComponentInParent<Block>();
	}

	private void Update()
	{
		img.fillAmount = block.counter / block.Cooldown();
	}
}
