using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Photon.Pun.Simple.Debugging;

public class UIConsole : MonoBehaviour
{
	public int maxSize = 3000;

	public bool logToDebug = true;

	public static readonly StringBuilder strb = new StringBuilder();

	private static UIConsole single;

	private static Text uitext;

	public static UIConsole Single
	{
		get
		{
			if (single == null)
			{
				CreateGUI();
			}
			return single;
		}
	}

	public UIConsole __
	{
		get
		{
			strb.Append(" ");
			return single;
		}
	}

	private void Awake()
	{
		single = this;
		uitext = GetComponent<Text>();
		uitext.text = strb.ToString();
	}

	public static void Log(string str)
	{
		if ((bool)single)
		{
			if (strb.Length > single.maxSize)
			{
				strb.Length = 0;
			}
			if (uitext != null)
			{
				strb.Append(str).Append("\n");
				uitext.text = strb.ToString();
			}
			if (single.logToDebug)
			{
				Debug.Log(str);
			}
		}
	}

	public UIConsole _(object str)
	{
		strb.Append(str.ToString());
		return single;
	}

	public UIConsole _(string str)
	{
		strb.Append(str);
		return single;
	}

	public UIConsole _(int str)
	{
		strb.Append(str);
		return single;
	}

	public UIConsole _(uint str)
	{
		strb.Append(str);
		return single;
	}

	public UIConsole _(byte str)
	{
		strb.Append(str);
		return single;
	}

	public UIConsole _(sbyte str)
	{
		strb.Append(str);
		return single;
	}

	public UIConsole _(short str)
	{
		strb.Append(str);
		return single;
	}

	public UIConsole _(ushort str)
	{
		strb.Append(str);
		return single;
	}

	public UIConsole _(long str)
	{
		strb.Append(str);
		return single;
	}

	public UIConsole _(ulong str)
	{
		strb.Append(str);
		return single;
	}

	public UIConsole _(float str)
	{
		strb.Append(str);
		return single;
	}

	public UIConsole _(double str)
	{
		strb.Append(str);
		return single;
	}

	public static void Refresh()
	{
		if ((bool)single && uitext != null)
		{
			uitext.text = strb.ToString();
		}
	}

	public static void Clear()
	{
		strb.Length = 0;
		if ((bool)uitext)
		{
			uitext.text = strb.ToString();
		}
	}

	public static UIConsole CreateGUI()
	{
		GameObject gameObject = new GameObject("UI CONSOLE");
		Canvas canvas = gameObject.AddComponent<Canvas>();
		GameObject gameObject2 = new GameObject("CONSOLE TEXT");
		gameObject2.transform.parent = gameObject.transform;
		uitext = gameObject2.AddComponent<Text>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		uitext.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
		uitext.verticalOverflow = VerticalWrapMode.Overflow;
		uitext.horizontalOverflow = HorizontalWrapMode.Overflow;
		uitext.alignment = TextAnchor.UpperCenter;
		uitext.rectTransform.pivot = new Vector2(0f, 0f);
		uitext.rectTransform.anchorMin = new Vector2(0f, 0f);
		uitext.rectTransform.anchorMax = new Vector2(1f, 1f);
		uitext.rectTransform.offsetMax = new Vector2(0f, 0f);
		single = gameObject2.AddComponent<UIConsole>();
		return single;
	}
}
