using TMPro;
using UnityEngine;

public class CrownPos : MonoBehaviour
{
	public TextMeshProUGUI text;

	public float GetOffset()
	{
		if (!(text.text == ""))
		{
			return 0.45f;
		}
		return 0f;
	}
}
