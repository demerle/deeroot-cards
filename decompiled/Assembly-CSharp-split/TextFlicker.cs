using TMPro;
using UnityEngine;

public class TextFlicker : MonoBehaviour
{
	public string[] strings;

	public float rate = 0.2f;

	private float counter;

	private int currentID;

	private TextMeshProUGUI text;

	private void Start()
	{
		text = GetComponent<TextMeshProUGUI>();
	}

	private void Update()
	{
		counter += Time.deltaTime;
		if (counter > rate)
		{
			counter = 0f;
			currentID++;
			if (currentID >= strings.Length)
			{
				currentID = 0;
			}
			text.text = strings[currentID];
		}
	}
}
