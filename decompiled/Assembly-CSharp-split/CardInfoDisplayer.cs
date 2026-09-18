using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardInfoDisplayer : MonoBehaviour
{
	public Color negativeColor;

	public Color positiveColor;

	public GameObject statObject;

	public GameObject grid;

	public GameObject chargeObj;

	public TextMeshProUGUI effectText;

	public TextMeshProUGUI nameText;

	public Image icon;

	public void DrawCard(CardInfoStat[] stats, string cardName, string description = "", Sprite image = null, bool charge = false)
	{
		if (charge)
		{
			chargeObj.SetActive(value: true);
			chargeObj.transform.SetParent(grid.transform, worldPositionStays: true);
		}
		if (description != "")
		{
			effectText.text = description;
			effectText.gameObject.SetActive(value: true);
			effectText.transform.SetParent(grid.transform, worldPositionStays: true);
		}
		nameText.text = cardName.ToUpper();
		for (int i = 0; i < stats.Length; i++)
		{
			GameObject obj = Object.Instantiate(statObject, grid.transform.position, grid.transform.rotation, grid.transform);
			obj.SetActive(value: true);
			obj.transform.localScale = Vector3.one;
			TextMeshProUGUI component = obj.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
			TextMeshProUGUI component2 = obj.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
			component.text = stats[i].stat;
			if (stats[i].simepleAmount != CardInfoStat.SimpleAmount.notAssigned && !Optionshandler.showCardStatNumbers)
			{
				component2.text = stats[i].GetSimpleAmount();
			}
			else
			{
				component2.text = stats[i].amount;
			}
			component2.color = (stats[i].positive ? positiveColor : negativeColor);
		}
		if ((bool)image)
		{
			icon.sprite = image;
		}
		effectText.transform.position += Vector3.up * 0.3f;
	}
}
