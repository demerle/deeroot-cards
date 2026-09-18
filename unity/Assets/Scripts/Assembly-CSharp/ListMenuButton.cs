using SoundImplementation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ListMenuButton : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler, IUpdateSelectedHandler, ISelectHandler
{
	private TextMeshProUGUI text;

	private Color defaultColor;

	private Vector3 defaultPos;

	public bool changeFontSize = true;

	public bool hideBar;

	public float setBarHeight;

	public bool toggleTextColor;

	public Color selectedTextColor;

	public Color defaultTextColor;

	private float smallFont = 40f;

	private bool inited;

	private void Awake()
	{
	}

	private void Start()
	{
		Init();
		smallFont = text.fontSize;
		defaultPos = text.transform.localPosition;
		defaultColor = text.color;
	}

	public void Nope()
	{
		Init();
		MenuEffects.instance.BlinkInColor(text, MenuEffects.instance.nopeColor, defaultColor, 0.15f);
		MenuEffects.instance.ShakeObject(text.gameObject, defaultPos, 15f, 0.2f);
	}

	public void Deselect()
	{
		Init();
		text.fontStyle = FontStyles.Normal;
		bool changeFontSize2 = changeFontSize;
		if (toggleTextColor)
		{
			GetComponentInChildren<TextMeshProUGUI>().color = defaultTextColor;
		}
	}

	public void Select()
	{
		Init();
		text.fontStyle = FontStyles.Bold;
		bool changeFontSize2 = changeFontSize;
		if (toggleTextColor)
		{
			GetComponentInChildren<TextMeshProUGUI>().color = selectedTextColor;
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		SoundPlayerStatic.Instance.PlayButtonClick();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		ListMenu.instance.SelectButton(this);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
	}

	public void OnUpdateSelected(BaseEventData eventData)
	{
	}

	public void OnSelect(BaseEventData eventData)
	{
		ListMenu.instance.SelectButton(this);
	}

	private void Init()
	{
		if (!inited)
		{
			inited = true;
			text = GetComponentInChildren<TextMeshProUGUI>();
		}
	}
}
