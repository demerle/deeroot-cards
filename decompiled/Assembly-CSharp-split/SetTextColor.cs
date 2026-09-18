using TMPro;
using UnityEngine;

public class SetTextColor : MonoBehaviour
{
	public Color[] colors;

	public void SetColor(int id)
	{
		GetComponent<TextMeshProUGUI>().color = colors[id];
	}
}
