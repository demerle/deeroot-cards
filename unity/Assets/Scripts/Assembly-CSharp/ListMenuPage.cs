using UnityEngine;

public class ListMenuPage : MonoBehaviour
{
	public ListMenuButton firstSelected;

	public float barHeight = 92f;

	private GameObject grid;

	private void Awake()
	{
		grid = base.transform.GetChild(0).gameObject;
	}

	public void Open()
	{
		ListMenu.instance.OpenPage(this);
		grid.SetActive(true);
	}

	public void Close()
	{
		DeselectAll();
		grid.SetActive(false);
	}

	private void DeselectAll()
	{
		ListMenuButton[] componentsInChildren = GetComponentsInChildren<ListMenuButton>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].Deselect();
		}
	}
}
