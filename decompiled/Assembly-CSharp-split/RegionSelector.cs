using TMPro;
using UnityEngine;

public class RegionSelector : MonoBehaviour
{
	private TMP_Dropdown dropDown;

	public static string region = "";

	private void Start()
	{
		dropDown = GetComponent<TMP_Dropdown>();
		region = dropDown.options[PlayerPrefs.GetInt("Region", 0)].text;
		dropDown.value = PlayerPrefs.GetInt("Region", 0);
		NetworkConnectionHandler.instance.hasRegionSelect = true;
	}

	public void OnValueChanged()
	{
		PlayerPrefs.SetInt("Region", dropDown.value);
		region = dropDown.options[dropDown.value].text;
	}
}
