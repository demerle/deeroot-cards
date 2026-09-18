using System;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardVisuals : MonoBehaviour
{
	private ScaleShake shake;

	public bool isSelected;

	private GeneralParticleSystem part;

	private Color selectedColor;

	private Color unSelectedColor = new Color(0.1f, 0.1f, 0.1f);

	public Color defaultColor;

	public Color chillColor;

	public Image[] images;

	public TextMeshProUGUI nameText;

	public GameObject statsObj;

	public GameObject[] objectsToToggle;

	private CardAnimation[] cardAnims;

	private CanvasGroup group;

	public bool firstValueToSet;

	public Action<bool> toggleSelectionAction;

	private void Start()
	{
		group = base.transform.Find("Canvas/Front/Grid").GetComponent<CanvasGroup>();
		selectedColor = GetComponentInParent<CardInfo>().cardColor;
		part = GetComponentInChildren<GeneralParticleSystem>();
		shake = GetComponent<ScaleShake>();
		Transform transform = base.transform.Find("Canvas/Front/Background/Art");
		CardInfo componentInParent = GetComponentInParent<CardInfo>();
		defaultColor = CardChoice.instance.GetCardColor(componentInParent.colorTheme);
		selectedColor = CardChoice.instance.GetCardColor2(componentInParent.colorTheme);
		if ((bool)componentInParent.cardArt)
		{
			GameObject obj = UnityEngine.Object.Instantiate(componentInParent.cardArt, transform.transform.position, transform.transform.rotation, transform);
			obj.transform.localPosition = Vector3.zero;
			obj.transform.SetAsFirstSibling();
			obj.transform.localScale = Vector3.one;
		}
		cardAnims = GetComponentsInChildren<CardAnimation>();
		isSelected = !firstValueToSet;
		ChangeSelected(firstValueToSet);
	}

	public void Leave()
	{
		UnityEngine.Object.Destroy(base.transform.root.gameObject);
	}

	public void Pick()
	{
		PhotonNetwork.Destroy(base.transform.root.gameObject);
	}

	public void ChangeSelected(bool setSelected)
	{
		if (!part || isSelected == setSelected)
		{
			return;
		}
		isSelected = setSelected;
		Action<bool> action = toggleSelectionAction;
		if (action != null)
		{
			action(isSelected);
		}
		if (isSelected)
		{
			part.simulationSpeedMultiplier = 1.25f;
			part.particleSettings.randomColor = selectedColor;
			shake.targetScale = 1.15f;
			group.alpha = 1f;
			for (int i = 0; i < images.Length; i++)
			{
				images[i].color = defaultColor;
			}
			for (int j = 0; j < objectsToToggle.Length; j++)
			{
				objectsToToggle[j].SetActive(false);
			}
			for (int k = 0; k < cardAnims.Length; k++)
			{
				cardAnims[k].enabled = true;
			}
			nameText.color = defaultColor;
			CurveAnimation[] componentsInChildren = GetComponentsInChildren<CurveAnimation>();
			for (int l = 0; l < componentsInChildren.Length; l++)
			{
				if (componentsInChildren[l].transform.parent != base.transform)
				{
					componentsInChildren[l].PlayIn();
				}
				else if (componentsInChildren[l].currentState != 0)
				{
					componentsInChildren[l].PlayIn();
				}
			}
			return;
		}
		part.simulationSpeedMultiplier = 0.5f;
		part.particleSettings.randomColor = unSelectedColor;
		shake.targetScale = 0.9f;
		group.alpha = 0.15f;
		for (int m = 0; m < images.Length; m++)
		{
			images[m].color = chillColor;
		}
		for (int n = 0; n < objectsToToggle.Length; n++)
		{
			objectsToToggle[n].SetActive(true);
		}
		for (int num = 0; num < cardAnims.Length; num++)
		{
			cardAnims[num].enabled = false;
		}
		nameText.color = chillColor;
		CurveAnimation[] componentsInChildren2 = GetComponentsInChildren<CurveAnimation>();
		for (int num2 = 0; num2 < componentsInChildren2.Length; num2++)
		{
			if (componentsInChildren2[num2].transform.parent != base.transform)
			{
				componentsInChildren2[num2].PlayOut();
			}
		}
	}
}
